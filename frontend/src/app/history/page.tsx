"use client";

import { useEffect } from "react";
import Link from "next/link";
import toast from "react-hot-toast";
import { useGetHistoryQuery } from "@/lib/api";

const verdictColor: Record<string, string> = {
  Correct: "text-emerald-400",
  "Partially Correct": "text-amber-400",
  Incorrect: "text-rose-400",
};

export default function HistoryPage() {
  const { data: attempts, isLoading, isError } = useGetHistoryQuery();

  useEffect(() => {
    if (isError) toast.error("Couldn't reach the API. Is the backend running?");
  }, [isError]);

  return (
    <div>
      <h1 className="mb-6 text-2xl font-bold tracking-tight">Attempt history</h1>

      {isLoading && <p className="text-white/50">Loading&hellip;</p>}
      {isError && <p className="text-rose-400">Couldn&rsquo;t reach the API.</p>}
      {attempts?.length === 0 && (
        <p className="text-white/50">
          No attempts yet &mdash; go solve a{" "}
          <Link href="/" className="text-emerald-400 hover:underline">
            prompt
          </Link>
          .
        </p>
      )}

      <div className="divide-y divide-white/10 overflow-hidden rounded-xl border border-white/10">
        {attempts?.map((attempt) => (
          <Link
            key={attempt.id}
            href={`/history/${attempt.id}`}
            className="flex items-center justify-between bg-white/[0.02] px-5 py-4 transition-colors hover:bg-white/[0.06]"
          >
            <div>
              <p className="font-medium">{attempt.promptTitle}</p>
              <p className="text-xs text-white/40">
                {new Date(attempt.createdAt).toLocaleString()}
              </p>
            </div>
            <div className="flex items-center gap-4">
              <span className={`text-sm font-medium ${verdictColor[attempt.verdict] ?? ""}`}>
                {attempt.verdict}
              </span>
              <span className="font-mono text-sm text-white/70">{attempt.score}/100</span>
            </div>
          </Link>
        ))}
      </div>
    </div>
  );
}
