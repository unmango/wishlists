import { createContext, useContext } from 'react';
import { type Client, defaultClient } from '../api';

const Context = createContext<Client>(defaultClient);

export function useApi(): Client {
  return useContext(Context);
}

export const ApiProvider = Context.Provider;
