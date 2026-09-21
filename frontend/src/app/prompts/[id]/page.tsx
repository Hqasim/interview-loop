import PromptWorkspace from "@/components/PromptWorkspace";

export default async function PromptPage(props: PageProps<"/prompts/[id]">) {
  const { id } = await props.params;
  return <PromptWorkspace promptId={Number(id)} />;
}
