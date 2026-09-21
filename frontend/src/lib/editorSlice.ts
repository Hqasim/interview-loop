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
    draftReset(state, action: PayloadAction<{ promptId: number }>) {
      delete state.draftsByPromptId[action.payload.promptId];
    },
  },
});

export const { draftChanged, languageChanged, draftReset } =
  editorSlice.actions;
export default editorSlice.reducer;
