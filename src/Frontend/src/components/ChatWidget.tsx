import CloseIcon from "@mui/icons-material/Close";
import RefreshIcon from "@mui/icons-material/Refresh";
import SendIcon from "@mui/icons-material/Send";
import SmartToyIcon from "@mui/icons-material/SmartToy";
import {
  Alert,
  Box,
  Chip,
  CircularProgress,
  Fab,
  IconButton,
  Paper,
  Stack,
  TextField,
  Tooltip,
  Typography,
} from "@mui/material";
import { FormEvent, useEffect, useRef, useState } from "react";

interface ChatMessage {
  role: "user" | "assistant";
  content: string;
}

interface ChatApiResponse {
  conversationId: string;
  reply: string;
}

interface ChatStatusResponse {
  configured: boolean;
  model: string;
}

const EXAMPLE_QUESTIONS = [
  "Quanti clienti ci sono nella categoria Garden?",
  "Quali fornitori hanno email su dominio gmail.com?",
  "Qual è l'IBAN del cliente Acquadro?",
];

export default function ChatWidget() {
  const [open, setOpen] = useState(false);
  const [messages, setMessages] = useState<ChatMessage[]>([]);
  const [input, setInput] = useState("");
  const [conversationId, setConversationId] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [chatConfigured, setChatConfigured] = useState<boolean | null>(null);
  const [chatModel, setChatModel] = useState<string | null>(null);
  const messagesEndRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (open) {
      messagesEndRef.current?.scrollIntoView({ behavior: "smooth" });
    }
  }, [messages, open, loading]);

  useEffect(() => {
    if (!open || chatConfigured !== null) return;

    fetch("/api/chat/status")
      .then((response) => response.json())
      .then((data) => {
        const status = data as ChatStatusResponse;
        setChatConfigured(status.configured);
        setChatModel(status.model);
      })
      .catch(() => {
        setChatConfigured(false);
      });
  }, [open, chatConfigured]);

  const startNewConversation = () => {
    setMessages([]);
    setConversationId(null);
    setInput("");
    setError(null);
  };

  const sendMessage = async (messageText: string) => {
    const trimmedInput = messageText.trim();
    if (!trimmedInput || loading) return;

    setInput("");
    setError(null);
    setMessages((current) => [...current, { role: "user", content: trimmedInput }]);
    setLoading(true);

    try {
      const response = await fetch("/api/chat", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          conversationId,
          message: trimmedInput,
        }),
      });

      const data = await response.json();

      if (!response.ok) {
        throw new Error(data.detail || data.error || data.title || "Chat request failed.");
      }

      const result = data as ChatApiResponse;
      setConversationId(result.conversationId);
      setMessages((current) => [...current, { role: "assistant", content: result.reply }]);
    } catch (requestError) {
      const message =
        requestError instanceof Error ? requestError.message : "Chat request failed.";
      setError(message);
    } finally {
      setLoading(false);
    }
  };

  const handleSubmit = (event: FormEvent) => {
    event.preventDefault();
    void sendMessage(input);
  };

  return (
    <>
      {open && (
        <Paper
          elevation={8}
          sx={{
            position: "fixed",
            right: 24,
            bottom: 96,
            width: { xs: "calc(100vw - 32px)", sm: 400 },
            maxWidth: 400,
            height: 520,
            display: "flex",
            flexDirection: "column",
            zIndex: (theme) => theme.zIndex.speedDial,
            overflow: "hidden",
          }}
        >
          <Box
            sx={{
              px: 2,
              py: 1.5,
              bgcolor: "primary.main",
              color: "primary.contrastText",
              display: "flex",
              alignItems: "center",
              gap: 1,
            }}
          >
            <SmartToyIcon fontSize="small" />
            <Box sx={{ flexGrow: 1 }}>
              <Typography variant="subtitle1">AI Assistant</Typography>
              {chatModel && (
                <Typography variant="caption" sx={{ opacity: 0.85 }}>
                  {chatModel}
                </Typography>
              )}
            </Box>
            <Tooltip title="New conversation">
              <IconButton size="small" color="inherit" onClick={startNewConversation}>
                <RefreshIcon fontSize="small" />
              </IconButton>
            </Tooltip>
            <IconButton size="small" color="inherit" onClick={() => setOpen(false)}>
              <CloseIcon fontSize="small" />
            </IconButton>
          </Box>

          <Box
            sx={{
              flexGrow: 1,
              overflowY: "auto",
              p: 2,
              bgcolor: "grey.50",
              display: "flex",
              flexDirection: "column",
              gap: 1.5,
            }}
          >
            {chatConfigured === false && (
              <Alert severity="warning">
                OpenAI is not configured. Add OPENAI_API_KEY to src/Backend/.env.local.
              </Alert>
            )}

            {messages.length === 0 && !loading && (
              <Box sx={{ display: "flex", flexDirection: "column", gap: 1.5, mt: 1 }}>
                <Typography variant="body2" color="text.secondary">
                  Ask about customers or suppliers in natural language. The assistant queries
                  live data from the database.
                </Typography>
                <Stack spacing={1}>
                  {EXAMPLE_QUESTIONS.map((question) => (
                    <Chip
                      key={question}
                      label={question}
                      variant="outlined"
                      onClick={() => void sendMessage(question)}
                      disabled={loading || chatConfigured === false}
                      sx={{ height: "auto", py: 1, "& .MuiChip-label": { whiteSpace: "normal" } }}
                    />
                  ))}
                </Stack>
              </Box>
            )}

            {messages.map((message, index) => (
              <Box
                key={`${message.role}-${index}`}
                sx={{
                  alignSelf: message.role === "user" ? "flex-end" : "flex-start",
                  maxWidth: "90%",
                }}
              >
                <Paper
                  sx={{
                    px: 1.5,
                    py: 1,
                    bgcolor: message.role === "user" ? "primary.main" : "background.paper",
                    color: message.role === "user" ? "primary.contrastText" : "text.primary",
                  }}
                >
                  <Typography variant="body2" sx={{ whiteSpace: "pre-wrap" }}>
                    {message.content}
                  </Typography>
                </Paper>
              </Box>
            ))}

            {loading && (
              <Stack direction="row" spacing={1} alignItems="center" sx={{ alignSelf: "flex-start" }}>
                <CircularProgress size={16} />
                <Typography variant="body2" color="text.secondary">
                  Querying data...
                </Typography>
              </Stack>
            )}

            <div ref={messagesEndRef} />
          </Box>

          {error && (
            <Alert severity="error" sx={{ mx: 2, mb: 1 }} onClose={() => setError(null)}>
              {error}
            </Alert>
          )}

          <Box
            component="form"
            onSubmit={handleSubmit}
            sx={{
              p: 2,
              borderTop: 1,
              borderColor: "divider",
              display: "flex",
              gap: 1,
            }}
          >
            <TextField
              fullWidth
              size="small"
              placeholder="Ask about customers or suppliers..."
              value={input}
              onChange={(event) => setInput(event.target.value)}
              disabled={loading || chatConfigured === false}
            />
            <IconButton
              type="submit"
              color="primary"
              disabled={loading || chatConfigured === false || !input.trim()}
            >
              <SendIcon />
            </IconButton>
          </Box>
        </Paper>
      )}

      <Fab
        color="primary"
        aria-label="Open AI chat"
        onClick={() => setOpen((current) => !current)}
        sx={{
          position: "fixed",
          right: 24,
          bottom: 24,
          zIndex: (theme) => theme.zIndex.speedDial,
        }}
      >
        {open ? <CloseIcon /> : <SmartToyIcon />}
      </Fab>
    </>
  );
}
