"use client";

import { useEffect } from "react";
import Link from "next/link";
import toast from "react-hot-toast";
import { useGetPromptsQuery } from "@/lib/api";
import DifficultyBadge from "@/components/DifficultyBadge";

/** Home route ("/") - the prompt catalog. Each card links to /prompts/[id]. */
export default function HomePage() {
  const { data: prompts, isLoading, isError } = useGetPromptsQuery();

  useEffect(() => {
    if (isError) toast.error("Couldn't reach the API. Is the backend running?");
  }, [isError]);

  return (
    <div>
      <div className="mb-10">
        <h1 className="text-3xl font-bold tracking-tight">Practice a coding interview</h1>
        <p className="mt-2 max-w-2xl text-white/60">
          Pick a prompt, write your solution, and get instant AI-graded feedback on
          correctness, complexity, and clarity &mdash; just like a real interview debrief.
        </p>
      </div>

      {isLoading && <p className="text-white/50">Loading prompts&hellip;</p>}
      {isError && (
        <p className="text-rose-400">
          Couldn&rsquo;t reach the API. Is the backend running?
        </p>
      )}

      <div className="grid gap-4 sm:grid-cols-2">
        {prompts?.map((prompt) => (
          <Link
            key={prompt.id}
            href={`/prompts/${prompt.id}`}
            className="group rounded-xl border border-white/10 bg-white/[0.03] p-5 transition-colors hover:border-white/25 hover:bg-white/[0.06]"
          >
            <div className="mb-3 flex items-center justify-between">
              <h2 className="font-semibold text-white group-hover:text-emerald-300">
                {prompt.title}
              </h2>
              <DifficultyBadge difficulty={prompt.difficulty} />
            </div>
            <p className="text-sm text-white/50">Start solving &rarr;</p>
          </Link>
        ))}
      </div>
    </div>
  );
}
