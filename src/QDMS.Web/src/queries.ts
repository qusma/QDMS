// Shared react-query hooks for the lists most pages need.

import { useQuery, useQueryClient } from '@tanstack/react-query';
import { api } from './api';

export const useInstruments = () => useQuery({ queryKey: ['instruments'], queryFn: api.instruments });
export const useExchanges = () => useQuery({ queryKey: ['exchanges'], queryFn: api.exchanges });
export const useDatasources = () => useQuery({ queryKey: ['datasources'], queryFn: api.datasources });
export const useTags = () => useQuery({ queryKey: ['tags'], queryFn: api.tags });
export const useSessionTemplates = () => useQuery({ queryKey: ['sessiontemplates'], queryFn: api.sessionTemplates });
export const useUnderlyingSymbols = () => useQuery({ queryKey: ['underlyingsymbols'], queryFn: api.underlyingSymbols });
export const useCurrencies = () => useQuery({ queryKey: ['currencies'], queryFn: api.currencies, staleTime: Infinity });
export const useTimezones = () => useQuery({ queryKey: ['timezones'], queryFn: api.timezones, staleTime: Infinity });
export const useStorageInfo = () => useQuery({ queryKey: ['storageinfo'], queryFn: api.storageInfo });

export function useInvalidate() {
  const client = useQueryClient();
  return (...keys: string[]) => Promise.all(keys.map((k) => client.invalidateQueries({ queryKey: [k] })));
}
