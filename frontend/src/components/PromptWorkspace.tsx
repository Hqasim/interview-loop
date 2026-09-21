"use client";

import { useEffect, useRef, useState } from "react";
import Editor, { type OnMount } from "@monaco-editor/react";
import toast from "react-hot-toast";
import {
  useGetPromptByIdQuery,
  useSubmitAttemptMutation,
  type AttemptFeedback,
} from "@/lib/api";
import { useAppDispatch, useAppSelector } from "@/lib/hooks";
import { draftChanged, draftReset, languageChanged } from "@/lib/editorSlice";
import { getErrorMessage } from "@/lib/errorMessage";
import DifficultyBadge from "@/components/DifficultyBadge";
import FeedbackPanel from "@/components/FeedbackPanel";

const LANGUAGES = ["javascript", "typescript", "python", "csharp", "java"];

// Client-side UX nicety, not the real throttle - see the backend's GradingThrottle rate
// limiter (RateLimiting.cs) for the actual enforcement, which this window matches.
const SUBMIT_COOLDOWN_MS = 3000;
const SLOW_RESPONSE_WARNING_MS = 5000;
const SLOW_TOAST_ID = "grading-slow";

export default function PromptWorkspace({ promptId }: { promptId: number }) {
  const dispatch = useAppDispatch();
  const { data: prompt, isLoading, isError } = useGetPromptByIdQuery(promptId);
  const [submitAttempt, { isLoading: isGrading }] = useSubmitAttemptMutation();

  const language = useAppSelector((s) => s.editor.language);
  const draft = useAppSelector((s) => s.editor.draftsByPromptId[promptId]);
  const [feedback, setFeedback] = useState<AttemptFeedback | null>(null);
  const [isCoolingDown, setIsCoolingDown] = useState(false);
  const editorRef = useRef<Parameters<OnMount>[0] | null>(null);

  useEffect(() => {
    if (prompt && draft === undefined && prompt.starterCode) {
      dispatch(draftChanged({ promptId, code: prompt.starterCode }));
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [prompt, promptId]);

  useEffect(() => {
    if (isError) toast.error("Couldn't load this prompt. Is the backend running?");
  }, [isError]);

  if (isLoading) return <p className="text-white/50">Loading prompt&hellip;</p>;
  if (isError || !prompt)
    return <p className="text-rose-400">Couldn&rsquo;t load this prompt.</p>;

  const code = draft ?? prompt.starterCode ?? "";

  const handleEditorMount: OnMount = (editorInstance) => {
    editorRef.current = editorInstance;
  };

  const handleReset = () => {
    dispatch(draftReset({ promptId }));
    setFeedback(null);
  };

  const handleReformat = () => {
    editorRef.current?.getAction("editor.action.formatDocument")?.run();
  };

  const handleSubmit = async () => {
    if (isGrading || isCoolingDown) return;

    setIsCoolingDown(true);
    setTimeout(() => setIsCoolingDown(false), SUBMIT_COOLDOWN_MS);

    setFeedback(null);

    const slowResponseTimer = setTimeout(() => {
      toast.loading("Hang in there, we are working on your grading.", { id: SLOW_TOAST_ID });
    }, SLOW_RESPONSE_WARNING_MS);

    try {
      const result = await submitAttempt({ promptId, code, language }).unwrap();
      setFeedback(result.feedback);
    } catch (err) {
      toast.error(getErrorMessage(err));
    } finally {
      clearTimeout(slowResponseTimer);
      toast.dismiss(SLOW_TOAST_ID);
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
        <div className="flex items-center gap-2">
          <button
            onClick={handleReset}
            className="rounded-md border border-white/10 bg-white/[0.05] px-3 py-1.5 text-sm text-white/80 transition-colors hover:bg-white/[0.1]"
          >
            Reset
          </button>
          <button
            onClick={handleReformat}
            className="rounded-md border border-white/10 bg-white/[0.05] px-3 py-1.5 text-sm text-white/80 transition-colors hover:bg-white/[0.1]"
          >
            Reformat
          </button>
          <button
            onClick={handleSubmit}
            disabled={isGrading || isCoolingDown}
            className="rounded-md bg-emerald-500 px-4 py-1.5 text-sm font-medium text-black transition-colors hover:bg-emerald-400 disabled:cursor-not-allowed disabled:opacity-50"
          >
            {isGrading ? "Grading…" : "Submit for AI feedback"}
          </button>
        </div>
      </div>

      <div className="overflow-hidden rounded-xl border border-white/10">
        <Editor
          height="420px"
          theme="vs-dark"
          language={language === "csharp" ? "csharp" : language}
          value={code}
          onMount={handleEditorMount}
          onChange={(value) =>
            dispatch(draftChanged({ promptId, code: value ?? "" }))
          }
          options={{ minimap: { enabled: false }, fontSize: 14, padding: { top: 16 } }}
        />
      </div>

      {feedback && (
        <div className="mt-6">
          <FeedbackPanel feedback={feedback} />
        </div>
      )}
    </div>
  );
}
