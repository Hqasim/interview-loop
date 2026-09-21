"use client";

import { useEffect, useState } from "react";
import Editor from "@monaco-editor/react";
import {
  useGetPromptByIdQuery,
  useSubmitAttemptMutation,
  type AttemptFeedback,
} from "@/lib/api";
import { useAppDispatch, useAppSelector } from "@/lib/hooks";
import { draftChanged, languageChanged } from "@/lib/editorSlice";
import DifficultyBadge from "@/components/DifficultyBadge";
import FeedbackPanel from "@/components/FeedbackPanel";

const LANGUAGES = ["javascript", "typescript", "python", "csharp", "java"];

export default function PromptWorkspace({ promptId }: { promptId: number }) {
  const dispatch = useAppDispatch();
  const { data: prompt, isLoading, isError } = useGetPromptByIdQuery(promptId);
  const [submitAttempt, { isLoading: isGrading }] = useSubmitAttemptMutation();

  const language = useAppSelector((s) => s.editor.language);
  const draft = useAppSelector((s) => s.editor.draftsByPromptId[promptId]);
  const [feedback, setFeedback] = useState<AttemptFeedback | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (prompt && draft === undefined && prompt.starterCode) {
      dispatch(draftChanged({ promptId, code: prompt.starterCode }));
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [prompt, promptId]);

  if (isLoading) return <p className="text-white/50">Loading prompt&hellip;</p>;
  if (isError || !prompt)
    return <p className="text-rose-400">Couldn&rsquo;t load this prompt.</p>;

  const code = draft ?? prompt.starterCode ?? "";

  const handleSubmit = async () => {
    setError(null);
    setFeedback(null);
    try {
      const result = await submitAttempt({ promptId, code, language }).unwrap();
      setFeedback(result.feedback);
    } catch {
      setError("Grading failed. Please try again in a moment.");
    }
  };

  return (
    <div>
      <div className="mb-6">
        <div className="mb-2 flex items-center gap-3">
          <h1 className="text-2xl font-bold tracking-tight">{prompt.title}</h1>
          <DifficultyBadge difficulty={prompt.difficulty} />
        </div>
        <p className="max-w-3xl text-white/60">{prompt.description}</p>
      </div>

      <div className="mb-3 flex items-center justify-between">
        <select
          value={language}
          onChange={(e) => dispatch(languageChanged(e.target.value))}
          className="rounded-md border border-white/10 bg-white/[0.05] px-3 py-1.5 text-sm"
        >
          {LANGUAGES.map((lang) => (
            <option key={lang} value={lang} className="bg-[#0b0d12]">
              {lang}
            </option>
          ))}
        </select>
        <button
          onClick={handleSubmit}
          disabled={isGrading}
          className="rounded-md bg-emerald-500 px-4 py-1.5 text-sm font-medium text-black transition-colors hover:bg-emerald-400 disabled:cursor-not-allowed disabled:opacity-50"
        >
          {isGrading ? "Grading…" : "Submit for AI feedback"}
        </button>
      </div>

      <div className="overflow-hidden rounded-xl border border-white/10">
        <Editor
          height="420px"
          theme="vs-dark"
          language={language === "csharp" ? "csharp" : language}
          value={code}
          onChange={(value) =>
            dispatch(draftChanged({ promptId, code: value ?? "" }))
          }
          options={{ minimap: { enabled: false }, fontSize: 14, padding: { top: 16 } }}
        />
      </div>

      {error && <p className="mt-4 text-rose-400">{error}</p>}

      {feedback && (
        <div className="mt-6">
          <FeedbackPanel feedback={feedback} />
        </div>
      )}
    </div>
  );
}
