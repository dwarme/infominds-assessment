import UploadFileIcon from "@mui/icons-material/UploadFile";
import {
  Alert,
  Box,
  Button,
  CircularProgress,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Typography,
} from "@mui/material";
import { useRef, useState } from "react";
import { DOCUMENT_FILE_ACCEPT, isAllowedDocumentFile, isWithinDocumentFileSizeLimit, MAX_DOCUMENT_FILE_SIZE_LABEL } from "../utils/documentFileTypes";

export interface UploadedDocument {
  id: number;
  title: string;
  fileType: string;
  uploadedAt: string;
}

interface DocumentUploadDialogProps {
  open: boolean;
  uploadUrl: string;
  onClose: () => void;
  onUploaded: (document: UploadedDocument) => void;
}

export default function DocumentUploadDialog({
  open,
  uploadUrl,
  onClose,
  onUploaded,
}: DocumentUploadDialogProps) {
  const fileInputRef = useRef<HTMLInputElement>(null);
  const [selectedFile, setSelectedFile] = useState<File | null>(null);
  const [previewContent, setPreviewContent] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [uploading, setUploading] = useState(false);

  const resetState = () => {
    setSelectedFile(null);
    setPreviewContent(null);
    setError(null);
    setUploading(false);
    if (fileInputRef.current) fileInputRef.current.value = "";
  };

  const handleClose = () => {
    resetState();
    onClose();
  };

  const handleFileChange = (file: File | null) => {
    setError(null);
    setPreviewContent(null);
    setSelectedFile(null);

    if (!file) return;

    if (!isAllowedDocumentFile(file)) {
      setError("Only .txt and .md files are allowed.");
      return;
    }

    if (!isWithinDocumentFileSizeLimit(file)) {
      setError(`File size exceeds the ${MAX_DOCUMENT_FILE_SIZE_LABEL} limit.`);
      return;
    }

    setSelectedFile(file);

    const reader = new FileReader();
    reader.onload = () => {
      setPreviewContent(typeof reader.result === "string" ? reader.result : null);
    };
    reader.onerror = () => {
      setError("Failed to read the selected file.");
    };
    reader.readAsText(file);
  };

  const handleUpload = async () => {
    if (!selectedFile) return;

    setUploading(true);
    setError(null);

    const formData = new FormData();
    formData.append("file", selectedFile);

    try {
      const response = await fetch(uploadUrl, {
        method: "POST",
        body: formData,
      });

      if (!response.ok) {
        const data = await response.json().catch(() => null);
        throw new Error(data?.error ?? "Upload failed.");
      }

      const document = (await response.json()) as UploadedDocument;
      onUploaded(document);
      handleClose();
    } catch (uploadError) {
      setError(uploadError instanceof Error ? uploadError.message : "Upload failed.");
    } finally {
      setUploading(false);
    }
  };

  return (
    <Dialog open={open} onClose={handleClose} fullWidth maxWidth="md">
      <DialogTitle>Upload document</DialogTitle>
      <DialogContent dividers>
        <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
          Select a .txt or .md file up to {MAX_DOCUMENT_FILE_SIZE_LABEL} to preview and upload.
        </Typography>

        <Box sx={{ mb: 2 }}>
          <input
            ref={fileInputRef}
            type="file"
            accept={DOCUMENT_FILE_ACCEPT}
            hidden
            onChange={(event) => handleFileChange(event.target.files?.[0] ?? null)}
          />
          <Button
            variant="outlined"
            startIcon={<UploadFileIcon />}
            onClick={() => fileInputRef.current?.click()}
            disabled={uploading}
          >
            Choose file
          </Button>
          {selectedFile && (
            <Typography sx={{ mt: 1 }}>
              Selected: {selectedFile.name}
            </Typography>
          )}
        </Box>

        {error && (
          <Alert severity="error" sx={{ mb: 2 }}>
            {error}
          </Alert>
        )}

        {previewContent !== null && (
          <Box>
            <Typography variant="subtitle2" sx={{ mb: 1 }}>
              Preview
            </Typography>
            <Typography
              component="pre"
              sx={{
                whiteSpace: "pre-wrap",
                wordBreak: "break-word",
                fontFamily: "inherit",
                m: 0,
                p: 2,
                bgcolor: "grey.100",
                borderRadius: 1,
                maxHeight: 320,
                overflow: "auto",
              }}
            >
              {previewContent}
            </Typography>
          </Box>
        )}
      </DialogContent>
      <DialogActions>
        <Button onClick={handleClose} disabled={uploading}>
          Cancel
        </Button>
        <Button
          variant="contained"
          onClick={handleUpload}
          disabled={!selectedFile || previewContent === null || uploading}
        >
          {uploading ? <CircularProgress size={20} /> : "Upload"}
        </Button>
      </DialogActions>
    </Dialog>
  );
}
