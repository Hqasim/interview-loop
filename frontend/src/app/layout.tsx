import type { Metadata } from "next";
import { Geist, Geist_Mono } from "next/font/google";
import Link from "next/link";
import { Toaster } from "react-hot-toast";
import "./globals.css";
import StoreProvider from "@/components/StoreProvider";

const geistSans = Geist({
  variable: "--font-geist-sans",
  subsets: ["latin"],
});

const geistMono = Geist_Mono({
  variable: "--font-geist-mono",
  subsets: ["latin"],
});

export const metadata: Metadata = {
  title: "InterviewLoop",
  description: "AI-graded mock coding interviews",
};

export default function RootLayout({ children }: LayoutProps<"/">) {
  return (
    <html
      lang="en"
      className={`${geistSans.variable} ${geistMono.variable} h-full antialiased`}
    >
      <body className="min-h-full flex flex-col bg-[var(--background)] text-[var(--foreground)]">
        <Toaster
          position="bottom-right"
          toastOptions={{
            style: {
              background: "#111827",
              color: "#e5e7eb",
              border: "1px solid rgba(255,255,255,0.1)",
              fontSize: "0.875rem",
              maxWidth: "24rem",
            },
            error: { iconTheme: { primary: "#fb7185", secondary: "#111827" } },
            success: { iconTheme: { primary: "#34d399", secondary: "#111827" } },
          }}
        />
        <StoreProvider>
          <header className="border-b border-white/10 bg-black/20 backdrop-blur">
            <nav className="mx-auto flex max-w-5xl items-center justify-between px-6 py-4">
              <Link href="/" className="flex items-center gap-2 font-semibold tracking-tight">
                <span className="inline-block h-2.5 w-2.5 rounded-full bg-emerald-400" />
                InterviewLoop
              </Link>
              <div className="flex gap-6 text-sm text-white/70">
                <Link href="/" className="hover:text-white transition-colors">
                  Prompts
                </Link>
                <Link href="/history" className="hover:text-white transition-colors">
                  History
                </Link>
                <a
                  href="https://github.com/Hqasim/interview-loop"
                  target="_blank"
                  rel="noreferrer"
                  className="hover:text-white transition-colors"
                >
                  GitHub
                </a>
              </div>
            </nav>
          </header>
          <main className="mx-auto w-full max-w-5xl flex-1 px-6 py-10">
            {children}
          </main>
          <footer className="border-t border-white/10 px-6 py-6 text-center text-xs text-white/40">
            Built with Next.js, Redux Toolkit, .NET, PostgreSQL &amp; Gemini · by Hamzah Qasim
          </footer>
        </StoreProvider>
      </body>
    </html>
  );
}
