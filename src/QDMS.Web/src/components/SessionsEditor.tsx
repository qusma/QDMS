import { DayOfTheWeek, enumOptions, type Session } from '../types';
import { EnumSelect } from '../ui';

const days = enumOptions(DayOfTheWeek);

/** Edits a list of trading sessions (exchange, template or instrument sessions). */
export default function SessionsEditor<T extends Session>(props: {
  sessions: T[];
  onChange: (sessions: T[]) => void;
  newSession: () => T;
  readOnly?: boolean;
}) {
  const update = (i: number, patch: Partial<Session>) =>
    props.onChange(props.sessions.map((s, j) => (j === i ? { ...s, ...patch } : s)));

  return (
    <div>
      <table className="simple">
        <thead>
          <tr>
            <th>Opening day</th>
            <th>Opens</th>
            <th>Closing day</th>
            <th>Closes</th>
            <th title="The session that ends the trading day (daily bars close here)">Ends day</th>
            <th />
          </tr>
        </thead>
        <tbody>
          {props.sessions.map((s, i) => (
            <tr key={i}>
              <td>
                {props.readOnly ? days[s.OpeningDay]?.label : (
                  <EnumSelect options={days} value={s.OpeningDay} onChange={(v) => update(i, { OpeningDay: (v ?? 0) as DayOfTheWeek })} />
                )}
              </td>
              <td>
                {props.readOnly ? toTime(s.OpeningAsSeconds) : (
                  <input type="time" step="1" value={toTime(s.OpeningAsSeconds)} onChange={(e) => update(i, { OpeningAsSeconds: toSeconds(e.target.value) })} />
                )}
              </td>
              <td>
                {props.readOnly ? days[s.ClosingDay]?.label : (
                  <EnumSelect options={days} value={s.ClosingDay} onChange={(v) => update(i, { ClosingDay: (v ?? 0) as DayOfTheWeek })} />
                )}
              </td>
              <td>
                {props.readOnly ? toTime(s.ClosingAsSeconds) : (
                  <input type="time" step="1" value={toTime(s.ClosingAsSeconds)} onChange={(e) => update(i, { ClosingAsSeconds: toSeconds(e.target.value) })} />
                )}
              </td>
              <td>
                <input type="checkbox" disabled={props.readOnly} checked={s.IsSessionEnd} onChange={(e) => update(i, { IsSessionEnd: e.target.checked })} />
              </td>
              <td>
                {!props.readOnly && (
                  <button className="danger" onClick={() => props.onChange(props.sessions.filter((_, j) => j !== i))}>Remove</button>
                )}
              </td>
            </tr>
          ))}
          {props.sessions.length === 0 && (
            <tr><td colSpan={6} className="muted">No sessions</td></tr>
          )}
        </tbody>
      </table>
      {!props.readOnly && (
        <div className="toolbar" style={{ marginTop: 8 }}>
          <button onClick={() => props.onChange([...props.sessions, props.newSession()])}>Add session</button>
          <button onClick={() => props.onChange([...props.sessions, ...weekdays(props.newSession)])}>Add Mon–Fri</button>
        </div>
      )}
    </div>
  );
}

const pad = (n: number) => String(n).padStart(2, '0');
/** seconds since midnight → "hh:mm:ss" */
const toTime = (s: number) => `${pad(Math.floor(s / 3600))}:${pad(Math.floor((s % 3600) / 60))}:${pad(Math.round(s % 60))}`;
/** <input type="time"> gives "hh:mm" or "hh:mm:ss" */
const toSeconds = (v: string) => {
  const [h = 0, m = 0, s = 0] = v.split(':').map(Number);
  return h * 3600 + m * 60 + s;
};

/** One session per weekday, copying the times of a fresh session (09:30–16:00 by default). */
function weekdays<T extends Session>(make: () => T): T[] {
  return [0, 1, 2, 3, 4].map((d) => ({ ...make(), OpeningDay: d as DayOfTheWeek, ClosingDay: d as DayOfTheWeek, IsSessionEnd: true }));
}

export const blankSession = (): Session => ({
  ID: 0,
  OpeningAsSeconds: 9.5 * 3600,
  ClosingAsSeconds: 16 * 3600,
  OpeningDay: 0,
  ClosingDay: 0,
  IsSessionEnd: true,
});
