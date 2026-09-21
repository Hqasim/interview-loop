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
