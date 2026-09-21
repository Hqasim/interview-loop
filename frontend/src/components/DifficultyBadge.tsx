const styles: Record<string, string> = {
  Easy: "bg-emerald-500/10 text-emerald-400 ring-emerald-500/30",
  Medium: "bg-amber-500/10 text-amber-400 ring-amber-500/30",
  Hard: "bg-rose-500/10 text-rose-400 ring-rose-500/30",
};

export default function DifficultyBadge({ difficulty }: { difficulty: string }) {
  const style = styles[difficulty] ?? "bg-white/10 text-white/70 ring-white/20";
  return (
    <span className={`inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium ring-1 ring-inset ${style}`}>
      {difficulty}
    </span>
  );
}
