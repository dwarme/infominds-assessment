import DownloadIcon from "@mui/icons-material/Download";
import {
  Box,
  Button,
  CircularProgress,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Typography,
} from "@mui/material";

interface DocumentContentModalProps {
  open: boolean;
  title: string;
  content: string | null;
  loading: boolean;
  onClose: () => void;
  onDownload?: () => void;
}

export default function DocumentContentModal({
  open,
  title,
  content,
  loading,
  onClose,
  onDownload,
}: DocumentContentModalProps) {
  return (
    <Dialog open={open} onClose={onClose} fullWidth maxWidth="md">
      <DialogTitle>{title}</DialogTitle>
      <DialogContent dividers>
        {loading ? (
          <Box sx={{ display: "flex", justifyContent: "center", py: 4 }}>
            <CircularProgress />
          </Box>
        ) : (
          <Typography
            component="pre"
            sx={{
              whiteSpace: "pre-wrap",
              wordBreak: "break-word",
              fontFamily: "inherit",
              m: 0,
            }}
          >
            {content ?? "No content available."}
          </Typography>
        )}
      </DialogContent>
      <DialogActions>
        {onDownload && (
          <Button onClick={onDownload} startIcon={<DownloadIcon />}>
            Download
          </Button>
        )}
        <Button onClick={onClose}>Close</Button>
      </DialogActions>
    </Dialog>
  );
}
