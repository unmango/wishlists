import { createContext, useContext } from "react";
import { defaultClient, type Client } from "../api";

const Context = createContext<Client>(defaultClient);

export function useApi(): Client {
	return useContext(Context);
}

export const ApiProvider = Context.Provider;
