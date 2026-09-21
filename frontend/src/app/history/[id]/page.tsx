import AttemptDetailView from "@/components/AttemptDetailView";

export default async function AttemptPage(props: PageProps<"/history/[id]">) {
  const { id } = await props.params;
  return <AttemptDetailView attemptId={Number(id)} />;
}
