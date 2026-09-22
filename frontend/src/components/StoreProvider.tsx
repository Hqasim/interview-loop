"use client";

import { useState } from "react";
import { Provider } from "react-redux";
import { makeStore } from "@/lib/store";

/**
 * Wraps the app in a Redux Provider with one store instance per mounted component tree (i.e.
 * per browser tab), created via useState's lazy initializer rather than useRef - the lazy
 * initializer form avoids reading/writing a ref during render, which the React Compiler-era
 * eslint rules (react-hooks/refs) flag as impure. See lib/store.ts for why makeStore is a
 * factory instead of a plain exported store instance.
 */
export default function StoreProvider({
  children,
}: {
  children: React.ReactNode;
}) {
  const [store] = useState(() => makeStore());

  return <Provider store={store}>{children}</Provider>;
}
