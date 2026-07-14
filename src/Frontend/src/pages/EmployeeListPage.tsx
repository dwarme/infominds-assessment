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
import ListPaginationFooter from "../components/ListPaginationFooter";
import StyledTableHeadCell from "../components/StyledTableHeadCell";
import { useDebouncedSearch } from "../hooks/useDebouncedSearch";
import { usePaginatedList } from "../hooks/usePaginatedList";

interface EmployeeDepartment {
  code: string;
  description: string;
}

interface EmployeeListItem {
  id: number;
  code: string;
  firstName: string;
  lastName: string;
  address: string;
  email: string;
  phone: string;
  department: EmployeeDepartment | null;
}

export default function EmployeeListPage() {
  const { searchText, setSearchText, debouncedSearchText } = useDebouncedSearch();
  const { list, page, setPage, totalCount, totalPages } =
    usePaginatedList<EmployeeListItem>("/api/employees/list", debouncedSearchText);

  return (
    <>
      <Typography variant="h4" sx={{ textAlign: "center", mt: 4, mb: 4 }}>
        Employees
      </Typography>

      <TextField
        label="Search: Name, Code, Email"
        variant="outlined"
        fullWidth
        value={searchText}
        onChange={(event) => setSearchText(event.target.value)}
        sx={{ mb: 4 }}
      />

      <TableContainer component={Paper}>
        <Table sx={{ minWidth: 650 }} aria-label="employees table">
          <TableHead>
            <TableRow>
              <StyledTableHeadCell>Code</StyledTableHeadCell>
              <StyledTableHeadCell>First name</StyledTableHeadCell>
              <StyledTableHeadCell>Last name</StyledTableHeadCell>
              <StyledTableHeadCell>Address</StyledTableHeadCell>
              <StyledTableHeadCell>Email</StyledTableHeadCell>
              <StyledTableHeadCell>Phone</StyledTableHeadCell>
              <StyledTableHeadCell>Department</StyledTableHeadCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {list.length === 0 ? (
              <TableRow>
                <TableCell colSpan={7} align="center">
                  No employees found
                </TableCell>
              </TableRow>
            ) : (
              list.map((row) => (
                <TableRow
                  key={row.id}
                  sx={{ "&:last-child td, &:last-child th": { border: 0 } }}
                >
                  <TableCell>{row.code}</TableCell>
                  <TableCell>{row.firstName}</TableCell>
                  <TableCell>{row.lastName}</TableCell>
                  <TableCell>{row.address}</TableCell>
                  <TableCell>{row.email}</TableCell>
                  <TableCell>{row.phone}</TableCell>
                  <TableCell>{row.department?.description ?? "-"}</TableCell>
                </TableRow>
              ))
            )}
          </TableBody>
        </Table>
        <ListPaginationFooter
          totalCount={totalCount}
          totalPages={totalPages}
          page={page}
          onPageChange={setPage}
          itemLabel="employees"
        />
      </TableContainer>
    </>
  );
}
