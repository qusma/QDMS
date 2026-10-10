import { useEffect, useRef } from 'react';
import {
  CandlestickSeries, ColorType, createChart, HistogramSeries,
  type IChartApi, type ISeriesApi, type UTCTimestamp,
} from 'lightweight-charts';
import type { OHLCBar } from '../types';

// The canvas can't use CSS variables, so the colors are read from the page's tokens (and re-read when the theme changes).
function themeColors() {
  const css = getComputedStyle(document.documentElement);
  const v = (name: string) => css.getPropertyValue(name).trim();
  return { bg: v('--panel'), fg: v('--muted'), grid: v('--border'), up: v('--ok'), down: v('--danger') };
}

// Bar times are exchange-local without a zone; the chart shows them as if they were UTC, i.e. unchanged.
const toTime = (dt: string) => (Date.parse(dt + 'Z') / 1000) as UTCTimestamp;

/** Candlestick chart of bars (raw or adjusted prices) with volume underneath. */
export default function BarChart(props: { bars: OHLCBar[]; adjusted: boolean }) {
  const el = useRef<HTMLDivElement>(null);
  const chart = useRef<IChartApi | null>(null);
  const candles = useRef<ISeriesApi<'Candlestick'> | null>(null);
  const volume = useRef<ISeriesApi<'Histogram'> | null>(null);

  useEffect(() => {
    if (!el.current) return;
    const c = themeColors();
    const ch = createChart(el.current, {
      autoSize: true,
      layout: { background: { type: ColorType.Solid, color: c.bg }, textColor: c.fg, attributionLogo: false },
      grid: { vertLines: { color: c.grid }, horzLines: { color: c.grid } },
      // a low minimum spacing lets fitContent show decades of daily bars
      timeScale: { timeVisible: true, secondsVisible: false, minBarSpacing: 0.01 },
    });
    const cs = ch.addSeries(CandlestickSeries, {
      upColor: c.up, downColor: c.down, borderUpColor: c.up, borderDownColor: c.down, wickUpColor: c.up, wickDownColor: c.down,
    });
    const vs = ch.addSeries(HistogramSeries, { priceScaleId: 'vol', priceFormat: { type: 'volume' }, color: c.grid });
    ch.priceScale('vol').applyOptions({ scaleMargins: { top: 0.8, bottom: 0 } });
    chart.current = ch;
    candles.current = cs;
    volume.current = vs;

    const media = window.matchMedia('(prefers-color-scheme: dark)');
    const onTheme = () => {
      const t = themeColors();
      ch.applyOptions({
        layout: { background: { type: ColorType.Solid, color: t.bg }, textColor: t.fg },
        grid: { vertLines: { color: t.grid }, horzLines: { color: t.grid } },
      });
      vs.applyOptions({ color: t.grid });
    };
    media.addEventListener('change', onTheme);

    return () => {
      media.removeEventListener('change', onTheme);
      ch.remove();
      chart.current = null;
    };
  }, []);

  useEffect(() => {
    if (!candles.current || !volume.current) return;
    const sorted = [...props.bars].sort((a, b) => (a.DT < b.DT ? -1 : a.DT > b.DT ? 1 : 0));
    candles.current.setData(sorted.map((b) => {
      const adj = props.adjusted && b.AdjClose !== null;
      return {
        time: toTime(b.DT),
        open: adj ? b.AdjOpen ?? b.Open : b.Open,
        high: adj ? b.AdjHigh ?? b.High : b.High,
        low: adj ? b.AdjLow ?? b.Low : b.Low,
        close: adj ? b.AdjClose ?? b.Close : b.Close,
      };
    }));
    volume.current.setData(sorted.map((b) => ({ time: toTime(b.DT), value: b.Volume ?? 0 })));
    chart.current?.timeScale().fitContent();
  }, [props.bars, props.adjusted]);

  return <div className="chart" ref={el} />;
}
