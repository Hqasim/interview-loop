"use client";

import { useEffect } from "react";
import Link from "next/link";
import Editor from "@monaco-editor/react";
import toast from "react-hot-toast";
import { useGetAttemptByIdQuery } from "@/lib/api";
import FeedbackPanel from "@/components/FeedbackPanel";

/**
 * Read-only replay of a past attempt: the exact code that was submitted (Monaco in
 * `readOnly` mode) plus its feedback, reusing FeedbackPanel so it looks identical to the
 * just-submitted view in PromptWorkspace.
 */
export default function AttemptDetailView({ attemptId }: { attemptId: number }) {
  const { data: attempt, isLoading, isError } = useGetAttemptByIdQuery(attemptId);

  useEffect(() => {
    if (isError) toast.error("Couldn't load this attempt. Is the backend running?");
  }, [isError]);

  if (isLoading) return <p className="text-white/50">Loading&hellip;</p>;
  if (isError || !attempt)
    return <p className="text-rose-400">Couldn&rsquo;t load this attempt.</p>;

  return (
    <div>
      <Link href="/history" className="text-sm text-white/50 hover:text-white">
        &larr; Back to history
      </Link>

      <h1 className="mb-1 mt-3 text-2xl font-bold tracking-tight">
        {attempt.promptTitle}
      </h1>
      <p className="mb-6 text-xs text-white/40">
        Submitted {new Date(attempt.createdAt).toLocaleString()} &middot; {attempt.language}
      </p>

      <div className="mb-6 overflow-hidden rounded-xl border border-white/10">
        <Editor
          height="360px"
          theme="vs-dark"
          language={attempt.language === "csharp" ? "csharp" : attempt.language}
          value={attempt.code}
          options={{ readOnly: true, minimap: { enabled: false }, fontSize: 14, padding: { top: 16 } }}
        />
      </div>

      <FeedbackPanel feedback={attempt.feedback} />
    </div>
  );
}
