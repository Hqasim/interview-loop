import AttemptDetailView from "@/components/AttemptDetailView";

// Server component wrapper - see prompts/[id]/page.tsx for why params is awaited here.
export default async function AttemptPage(props: PageProps<"/history/[id]">) {
  const { id } = await props.params;
  return <AttemptDetailView attemptId={Number(id)} />;
}
