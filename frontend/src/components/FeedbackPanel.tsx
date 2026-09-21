import type { AttemptFeedback } from "@/lib/api";

const verdictStyles: Record<AttemptFeedback["verdict"], string> = {
  Correct: "text-emerald-400 bg-emerald-500/10 ring-emerald-500/30",
  "Partially Correct": "text-amber-400 bg-amber-500/10 ring-amber-500/30",
  Incorrect: "text-rose-400 bg-rose-500/10 ring-rose-500/30",
};

export default function FeedbackPanel({ feedback }: { feedback: AttemptFeedback }) {
  return (
    <div className="rounded-xl border border-white/10 bg-white/[0.03] p-5">
      <div className="mb-4 flex items-center justify-between">
        <h3 className="font-semibold">AI Feedback</h3>
        <span
          className={`inline-flex items-center rounded-full px-3 py-1 text-xs font-medium ring-1 ring-inset ${verdictStyles[feedback.verdict]}`}
        >
          {feedback.verdict}
        </span>
      </div>

      <div className="mb-5 flex items-center gap-3">
        <div className="h-2 flex-1 overflow-hidden rounded-full bg-white/10">
          <div
            className="h-full rounded-full bg-gradient-to-r from-emerald-500 to-emerald-300"
            style={{ width: `${feedback.score}%` }}
          />
        </div>
        <span className="w-12 text-right font-mono text-sm text-white/70">
          {feedback.score}/100
        </span>
      </div>

      <dl className="space-y-4 text-sm">
        <div>
          <dt className="mb-1 font-medium text-white/80">Correctness</dt>
          <dd className="text-white/60">{feedback.correctnessNotes}</dd>
        </div>
        <div>
          <dt className="mb-1 font-medium text-white/80">Time &amp; Space Complexity</dt>
          <dd className="text-white/60">{feedback.complexityNotes}</dd>
        </div>
        <div>
          <dt className="mb-1 font-medium text-white/80">Code Clarity</dt>
          <dd className="text-white/60">{feedback.clarityNotes}</dd>
        </div>
        <div>
          <dt className="mb-1 font-medium text-white/80">Suggestions</dt>
          <dd className="text-white/60">{feedback.suggestions}</dd>
        </div>
      </dl>
    </div>
  );
}
