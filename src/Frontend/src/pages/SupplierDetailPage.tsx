import {
  Box,
  Button,
  IconButton,
  Paper,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Typography,
} from "@mui/material";
import DownloadIcon from "@mui/icons-material/Download";
import { useEffect, useState } from "react";
import { Link as RouterLink, useParams } from "react-router-dom";
import DocumentContentModal from "../components/DocumentContentModal";
import DocumentUploadDialog, { UploadedDocument } from "../components/DocumentUploadDialog";
import StyledTableHeadCell from "../components/StyledTableHeadCell";
import { downloadDocument } from "../utils/downloadDocument";

interface SupplierDetail {
  id: number;
  name: string;
  address: string;
  email: string;
  phone: string;
}

interface DocumentListItem {
  id: number;
  title: string;
  fileType: string;
  uploadedAt: string;
}

export default function SupplierDetailPage() {
  const { id } = useParams();
  const supplierId = Number(id);

  const [supplier, setSupplier] = useState<SupplierDetail | null>(null);
  const [documents, setDocuments] = useState<DocumentListItem[]>([]);
  const [notFound, setNotFound] = useState(false);

  const [modalOpen, setModalOpen] = useState(false);
  const [selectedDocument, setSelectedDocument] = useState<DocumentListItem | null>(null);
  const [modalContent, setModalContent] = useState<string | null>(null);
  const [loadingContent, setLoadingContent] = useState(false);
  const [uploadOpen, setUploadOpen] = useState(false);

  const loadDocuments = () => {
    fetch(`/api/suppliers/${supplierId}/documents`)
      .then((response) => response.json())
      .then((data) => {
        setDocuments((data.items ?? []) as DocumentListItem[]);
      });
  };

  useEffect(() => {
    if (!supplierId) return;

    fetch(`/api/suppliers/${supplierId}`)
      .then((response) => {
        if (response.status === 404) {
          setNotFound(true);
          return null;
        }
        return response.json();
      })
      .then((data) => {
        if (data) setSupplier(data as SupplierDetail);
      });

    loadDocuments();
  }, [supplierId]);

  const handleDocumentClick = (document: DocumentListItem) => {
    setSelectedDocument(document);
    setModalContent(null);
    setModalOpen(true);
    setLoadingContent(true);

    fetch(`/api/suppliers/${supplierId}/documents/${document.id}/content`)
      .then((response) => {
        if (!response.ok) throw new Error("Failed to load content");
        return response.json();
      })
      .then((data) => {
        setModalContent(data.content);
      })
      .catch(() => {
        setModalContent("Failed to load document content.");
      })
      .finally(() => {
        setLoadingContent(false);
      });
  };

  const handleDownload = (document: DocumentListItem) => {
    downloadDocument(
      `/api/suppliers/${supplierId}/documents/${document.id}`,
      document.title,
    );
  };

  const handleUploaded = (document: UploadedDocument) => {
    setDocuments((current) => [document, ...current]);
  };

  if (notFound) {
    return (
      <Typography variant="h5" sx={{ textAlign: "center", mt: 4 }}>
        Supplier not found.{" "}
        <Button component={RouterLink} to="/SupplierList">
          Back to list
        </Button>
      </Typography>
    );
  }

  if (!supplier) {
    return (
      <Typography variant="h6" sx={{ textAlign: "center", mt: 4 }}>
        Loading...
      </Typography>
    );
  }

  return (
    <>
      <Box sx={{ mt: 4, mb: 2 }}>
        <Button component={RouterLink} to="/SupplierList" variant="outlined">
          Back to suppliers
        </Button>
      </Box>

      <Typography variant="h4" sx={{ textAlign: "center", mb: 4 }}>
        {supplier.name}
      </Typography>

      <Paper sx={{ p: 3, mb: 4 }}>
        <Typography variant="h6" sx={{ mb: 2 }}>
          Supplier details
        </Typography>
        <Typography><strong>Address:</strong> {supplier.address}</Typography>
        <Typography><strong>Email:</strong> {supplier.email}</Typography>
        <Typography><strong>Phone:</strong> {supplier.phone}</Typography>
      </Paper>

      <Box sx={{ display: "flex", alignItems: "center", justifyContent: "space-between", mb: 2 }}>
        <Typography variant="h6">Documents</Typography>
        <Button variant="contained" onClick={() => setUploadOpen(true)}>
          Upload document
        </Button>
      </Box>

      <TableContainer component={Paper}>
        <Table sx={{ minWidth: 650 }} aria-label="documents table">
          <TableHead>
            <TableRow>
              <StyledTableHeadCell>Title</StyledTableHeadCell>
              <StyledTableHeadCell>Type</StyledTableHeadCell>
              <StyledTableHeadCell>Uploaded at</StyledTableHeadCell>
              <StyledTableHeadCell align="right" />
            </TableRow>
          </TableHead>
          <TableBody>
            {documents.length === 0 ? (
              <TableRow>
                <TableCell colSpan={4} align="center">
                  No documents
                </TableCell>
              </TableRow>
            ) : (
              documents.map((document) => (
                <TableRow
                  key={document.id}
                  hover
                  onClick={() => handleDocumentClick(document)}
                  sx={{ cursor: "pointer" }}
                >
                  <TableCell>{document.title}</TableCell>
                  <TableCell>{document.fileType}</TableCell>
                  <TableCell>
                    {new Date(document.uploadedAt).toLocaleString()}
                  </TableCell>
                  <TableCell align="right" onClick={(event) => event.stopPropagation()}>
                    <IconButton
                      aria-label={`Download ${document.title}`}
                      onClick={() => handleDownload(document)}
                      size="small"
                    >
                      <DownloadIcon />
                    </IconButton>
                  </TableCell>
                </TableRow>
              ))
            )}
          </TableBody>
        </Table>
      </TableContainer>

      <DocumentContentModal
        open={modalOpen}
        title={selectedDocument?.title ?? ""}
        content={modalContent}
        loading={loadingContent}
        onClose={() => setModalOpen(false)}
        onDownload={
          selectedDocument
            ? () => handleDownload(selectedDocument)
            : undefined
        }
      />

      <DocumentUploadDialog
        open={uploadOpen}
        uploadUrl={`/api/suppliers/${supplierId}/documents`}
        onClose={() => setUploadOpen(false)}
        onUploaded={handleUploaded}
      />
    </>
  );
}
