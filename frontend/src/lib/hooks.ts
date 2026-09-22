// Type-safe wrappers around react-redux's useDispatch/useSelector, pre-bound to this app's
// store types. Use these everywhere instead of the plain react-redux hooks, so dispatch() and
// selector state are fully typed without repeating the generic annotation at every call site.
import { useDispatch, useSelector, type TypedUseSelectorHook } from "react-redux";
import type { AppDispatch, RootState } from "./store";

export const useAppDispatch: () => AppDispatch = useDispatch;
export const useAppSelector: TypedUseSelectorHook<RootState> = useSelector;
