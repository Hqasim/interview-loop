/**
 * Redux Toolkit slice for the code editor's client-only state - what RTK Query can't own,
 * since it isn't server data. Drafts are keyed by prompt id so switching between prompts (or
 * navigating away and back) doesn't lose in-progress code; `draftReset` deletes a prompt's
 * entry entirely rather than overwriting it, which lets PromptWorkspace fall back to the
 * prompt's original starter code for free (see its `code = draft ?? prompt.starterCode ?? ""`).
 */
import { createSlice, type PayloadAction } from "@reduxjs/toolkit";

interface EditorState {
  language: string;
  draftsByPromptId: Record<number, string>;
}

const initialState: EditorState = {
  language: "javascript",
  draftsByPromptId: {},
};

const editorSlice = createSlice({
  name: "editor",
  initialState,
  reducers: {
    draftChanged(
      state,
      action: PayloadAction<{ promptId: number; code: string }>
    ) {
      state.draftsByPromptId[action.payload.promptId] = action.payload.code;
    },
    languageChanged(state, action: PayloadAction<string>) {
      state.language = action.payload;
    },
    /** Used by the Reset button - clears a prompt's draft so the UI falls back to starter code. */
    draftReset(state, action: PayloadAction<{ promptId: number }>) {
      delete state.draftsByPromptId[action.payload.promptId];
    },
  },
});

export const { draftChanged, languageChanged, draftReset } =
  editorSlice.actions;
export default editorSlice.reducer;
