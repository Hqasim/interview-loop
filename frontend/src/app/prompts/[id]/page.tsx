import PromptWorkspace from "@/components/PromptWorkspace";

// Server component wrapper: awaits the dynamic route's params (a promise as of Next.js 15+)
// and hands off a plain number to the client component that does the actual rendering.
export default async function PromptPage(props: PageProps<"/prompts/[id]">) {
  const { id } = await props.params;
  return <PromptWorkspace promptId={Number(id)} />;
}
