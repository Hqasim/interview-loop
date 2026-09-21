import type { FetchBaseQueryError } from "@reduxjs/toolkit/query";
import type { SerializedError } from "@reduxjs/toolkit";

function isFetchBaseQueryError(error: unknown): error is FetchBaseQueryError {
  return typeof error === "object" && error !== null && "status" in error;
}

/**
 * Extracts a human-readable message from an RTK Query error, regardless of whether the
 * backend returned a JSON {message} body, a plain string body, or nothing at all (network
 * failure). Falls back to a generic message rather than ever showing "[object Object]".
 */
export function getErrorMessage(error: unknown): string {
  if (!error) return "Something went wrong. Please try again.";

  if (isFetchBaseQueryError(error)) {
    const { data } = error;

    if (typeof data === "string" && data.trim()) return data;

    if (data && typeof data === "object" && "message" in data) {
      const message = (data as { message?: unknown }).message;
      if (typeof message === "string" && message.trim()) return message;
    }

    if (error.status === "FETCH_ERROR") {
      return "Couldn't reach the server. Is the backend running?";
    }
    if (error.status === "TIMEOUT_ERROR") {
      return "The request timed out. Please try again.";
    }
    if (error.status === 429) {
      return "You're submitting too quickly - please wait a few seconds and try again.";
    }
    if (typeof error.status === "number") {
      return `Request failed (${error.status}). Please try again.`;
    }
  }

  const serialized = error as SerializedError;
  if (serialized?.message) return serialized.message;

  return "Something went wrong. Please try again.";
}
