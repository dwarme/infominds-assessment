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
  styled,
  tableCellClasses,
} from "@mui/material";
import DownloadIcon from "@mui/icons-material/Download";
import { useEffect, useState } from "react";
import { Link as RouterLink, useParams } from "react-router-dom";
import DocumentContentModal from "../components/DocumentContentModal";
import { downloadDocument } from "../utils/downloadDocument";

interface CustomerCategory {
  id: number;
  code: string;
  description: string;
}

interface CustomerDetail {
  id: number;
  name: string;
  address: string;
  email: string;
  phone: string;
  iban: string;
  customerCategory: CustomerCategory | null;
}

interface DocumentListItem {
  id: number;
  title: string;
  fileType: string;
  uploadedAt: string;
}

export default function CustomerDetailPage() {
  const { id } = useParams();
  const customerId = Number(id);

  const [customer, setCustomer] = useState<CustomerDetail | null>(null);
  const [documents, setDocuments] = useState<DocumentListItem[]>([]);
  const [notFound, setNotFound] = useState(false);

  const [modalOpen, setModalOpen] = useState(false);
  const [selectedDocument, setSelectedDocument] = useState<DocumentListItem | null>(null);
  const [modalContent, setModalContent] = useState<string | null>(null);
  const [loadingContent, setLoadingContent] = useState(false);

  useEffect(() => {
    if (!customerId) return;

    fetch(`/api/customers/${customerId}`)
      .then((response) => {
        if (response.status === 404) {
          setNotFound(true);
          return null;
        }
        return response.json();
      })
      .then((data) => {
        if (data) setCustomer(data as CustomerDetail);
      });

    fetch(`/api/customers/${customerId}/documents`)
      .then((response) => response.json())
      .then((data) => {
        setDocuments((data.items ?? []) as DocumentListItem[]);
      });
  }, [customerId]);

  const handleDocumentClick = (document: DocumentListItem) => {
    setSelectedDocument(document);
    setModalContent(null);
    setModalOpen(true);
    setLoadingContent(true);

    fetch(`/api/customers/${customerId}/documents/${document.id}/content`)
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
      `/api/customers/${customerId}/documents/${document.id}`,
      document.title,
    );
  };

  if (notFound) {
    return (
      <Typography variant="h5" sx={{ textAlign: "center", mt: 4 }}>
        Customer not found.{" "}
        <Button component={RouterLink} to="/CustomerList">
          Back to list
        </Button>
      </Typography>
    );
  }

  if (!customer) {
    return (
      <Typography variant="h6" sx={{ textAlign: "center", mt: 4 }}>
        Loading...
      </Typography>
    );
  }

  return (
    <>
      <Box sx={{ mt: 4, mb: 2 }}>
        <Button component={RouterLink} to="/CustomerList" variant="outlined">
          Back to customers
        </Button>
      </Box>

      <Typography variant="h4" sx={{ textAlign: "center", mb: 4 }}>
        {customer.name}
      </Typography>

      <Paper sx={{ p: 3, mb: 4 }}>
        <Typography variant="h6" sx={{ mb: 2 }}>
          Customer details
        </Typography>
        <Typography><strong>Address:</strong> {customer.address}</Typography>
        <Typography><strong>Email:</strong> {customer.email}</Typography>
        <Typography><strong>Phone:</strong> {customer.phone}</Typography>
        <Typography><strong>IBAN:</strong> {customer.iban}</Typography>
        <Typography>
          <strong>Category:</strong> {customer.customerCategory?.description ?? "-"}
        </Typography>
      </Paper>

      <Typography variant="h6" sx={{ mb: 2 }}>
        Documents
      </Typography>

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
    </>
  );
}

const StyledTableHeadCell = styled(TableCell)(({ theme }) => ({
  [`&.${tableCellClasses.head}`]: {
    backgroundColor: theme.palette.primary.light,
    color: theme.palette.common.white,
  },
}));
