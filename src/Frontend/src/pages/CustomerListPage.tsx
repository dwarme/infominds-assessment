import {
  Paper,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  TextField,
  Typography,
} from "@mui/material";
import { Link as RouterLink } from "react-router-dom";
import ListPaginationFooter from "../components/ListPaginationFooter";
import StyledTableHeadCell from "../components/StyledTableHeadCell";
import { useDebouncedSearch } from "../hooks/useDebouncedSearch";
import { usePaginatedList } from "../hooks/usePaginatedList";

interface CustomerCategory {
  id: number;
  code: string;
  description: string;
}

interface CustomerListItem {
  id: number;
  name: string;
  address: string;
  email: string;
  phone: string;
  iban: string;
  customerCategory: CustomerCategory | null;
}

export default function CustomerListPage() {
  const { searchText, setSearchText, debouncedSearchText } = useDebouncedSearch();
  const { list, page, setPage, totalCount, totalPages } =
    usePaginatedList<CustomerListItem>("/api/customers/list", debouncedSearchText);

  return (
    <>
      <Typography variant="h4" sx={{ textAlign: "center", mt: 4, mb: 4 }}>
        Customers
      </Typography>

      <TextField
        label="Search: Name, Email"
        variant="outlined"
        fullWidth
        value={searchText}
        onChange={(event) => setSearchText(event.target.value)}
        sx={{ mb: 4 }}
      />

      <TableContainer component={Paper}>
        <Table sx={{ minWidth: 650 }} aria-label="simple table">
          <TableHead>
            <TableRow>
              <StyledTableHeadCell>Name</StyledTableHeadCell>
              <StyledTableHeadCell>Address</StyledTableHeadCell>
              <StyledTableHeadCell>Email</StyledTableHeadCell>
              <StyledTableHeadCell>Phone</StyledTableHeadCell>
              <StyledTableHeadCell>IBAN</StyledTableHeadCell>
              <StyledTableHeadCell>Customer Category</StyledTableHeadCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {list.map((row) => (
              <TableRow
                key={row.id}
                sx={{ "&:last-child td, &:last-child th": { border: 0 } }}
              >
                <TableCell>
                  <RouterLink to={`/customers/${row.id}`}>{row.name}</RouterLink>
                </TableCell>
                <TableCell>{row.address}</TableCell>
                <TableCell>{row.email}</TableCell>
                <TableCell>{row.phone}</TableCell>
                <TableCell>{row.iban}</TableCell>
                <TableCell>{row.customerCategory?.description ?? "-"}</TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
        <ListPaginationFooter
          totalCount={totalCount}
          totalPages={totalPages}
          page={page}
          onPageChange={setPage}
          itemLabel="customers"
        />
      </TableContainer>
    </>
  );
}
