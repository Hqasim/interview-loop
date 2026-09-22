/**
 * Redux store setup. `makeStore` is a factory (not a singleton) deliberately - the App Router
 * renders on the server, and a module-level singleton store would leak state between different
 * users' requests. StoreProvider.tsx creates one instance per browser session via this factory.
 */
import { configureStore } from "@reduxjs/toolkit";
import { api } from "./api";
import editorReducer from "./editorSlice";

export const makeStore = () =>
  configureStore({
    reducer: {
      [api.reducerPath]: api.reducer,
      editor: editorReducer,
    },
    // RTK Query's middleware enables caching, invalidation, and refetch-on-focus/reconnect.
    middleware: (getDefaultMiddleware) =>
      getDefaultMiddleware().concat(api.middleware),
  });

export type AppStore = ReturnType<typeof makeStore>;
export type RootState = ReturnType<AppStore["getState"]>;
export type AppDispatch = AppStore["dispatch"];
