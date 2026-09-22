/**
 * RTK Query API slice - the single source of truth for every network call this app makes.
 * Each hook below (useGetPromptsQuery, etc.) handles caching, loading/error state, and
 * refetching automatically; components never call fetch() directly. `submitAttempt`
 * invalidating the "Attempts" tag is what makes the history list refresh itself after a submit.
 *
 * These types mirror the backend's DTOs (backend/InterviewLoop.Api/Dtos) field-for-field -
 * there's no shared codegen between the two projects, so keep them in sync by hand if either
 * side's shape changes.
 */
import { createApi, fetchBaseQuery } from "@reduxjs/toolkit/query/react";

export interface PromptSummary {
  id: number;
  title: string;
  difficulty: "Easy" | "Medium" | "Hard";
}

export interface PromptDetail extends PromptSummary {
  description: string;
  starterCode: string | null;
}

export interface AttemptFeedback {
  score: number;
  verdict: "Correct" | "Partially Correct" | "Incorrect";
  correctnessNotes: string;
  complexityNotes: string;
  clarityNotes: string;
  suggestions: string;
}

export interface AttemptDetail {
  id: number;
  promptId: number;
  promptTitle: string;
  code: string;
  language: string;
  createdAt: string;
  feedback: AttemptFeedback;
}

export interface AttemptSummary {
  id: number;
  promptId: number;
  promptTitle: string;
  score: number;
  verdict: AttemptFeedback["verdict"];
  createdAt: string;
}

export interface SubmitAttemptRequest {
  promptId: number;
  code: string;
  language: string;
}

// NEXT_PUBLIC_* vars are inlined into the client bundle at build time (see frontend/.env.local
// for local dev, and the Amplify app's environment variables for production) - the localhost
// fallback here only ever applies if that build-time value was somehow missing.
const API_BASE_URL =
  process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5080/api";

export const api = createApi({
  reducerPath: "api",
  baseQuery: fetchBaseQuery({ baseUrl: API_BASE_URL }),
  tagTypes: ["Attempts"],
  endpoints: (builder) => ({
    getPrompts: builder.query<PromptSummary[], void>({
      query: () => "/prompts",
    }),
    getPromptById: builder.query<PromptDetail, number>({
      query: (id) => `/prompts/${id}`,
    }),
    submitAttempt: builder.mutation<AttemptDetail, SubmitAttemptRequest>({
      query: (body) => ({
        url: "/attempts",
        method: "POST",
        body,
      }),
      invalidatesTags: ["Attempts"],
    }),
    getHistory: builder.query<AttemptSummary[], void>({
      query: () => "/attempts",
      providesTags: ["Attempts"],
    }),
    getAttemptById: builder.query<AttemptDetail, number>({
      query: (id) => `/attempts/${id}`,
    }),
  }),
});

export const {
  useGetPromptsQuery,
  useGetPromptByIdQuery,
  useSubmitAttemptMutation,
  useGetHistoryQuery,
  useGetAttemptByIdQuery,
} = api;
