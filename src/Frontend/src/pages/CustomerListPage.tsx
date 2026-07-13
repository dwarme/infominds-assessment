import {
  Box,
  Paper,
  Pagination,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  TextField,
  Typography,
  styled,
  tableCellClasses,
} from "@mui/material";
import { useEffect, useMemo, useState } from "react";
import { Link as RouterLink } from "react-router-dom";
import { debounce } from "../utils/debounce";

interface CustomerCategory {
  id: number;
  code: string;
  description: string;
}

interface CustomerListQuery {
  id: number;
  name: string;
  address: string;
  email: string;
  phone: string;
  iban: string;
  customerCategory: CustomerCategory | null;
}

interface CustomersListPaginatedResponse {
  items: CustomerListQuery[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

const PAGE_SIZE = 50;

export default function CustomerListPage() {
  const [list, setList] = useState<CustomerListQuery[]>([]);
  const [searchText, setSearchText] = useState("");
  const [debouncedSearchText, setDebouncedSearchText] = useState("");
  const [page, setPage] = useState(1);
  const [totalCount, setTotalCount] = useState(0);
  const [totalPages, setTotalPages] = useState(0);

  const debouncedSetSearch = useMemo(
    () =>
      debounce((search: string) => {
        setDebouncedSearchText(search);
      }, 300),
    []
  );

  useEffect(() => {
    debouncedSetSearch(searchText);

    return () => {
      debouncedSetSearch.cancel();
    };
  }, [searchText, debouncedSetSearch]);

  useEffect(() => {
    setPage(1);
  }, [debouncedSearchText]);

  useEffect(() => {
    const params = new URLSearchParams();
    if (debouncedSearchText) {
      params.set("SearchText", debouncedSearchText);
    }
    params.set("Page", String(page));
    params.set("PageSize", String(PAGE_SIZE));

    fetch(`/api/customers/list?${params.toString()}`)
      .then((response) => response.json())
      .then((data) => {
        const result = data as CustomersListPaginatedResponse;
        setList(result.items);
        setTotalCount(result.totalCount);
        setTotalPages(result.totalPages);
      });
  }, [debouncedSearchText, page]);

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
        <Box
          sx={{
            display: "flex",
            flexDirection: "column",
            alignItems: "center",
            gap: 1,
            py: 2,
          }}
        >
          <Typography variant="body2" color="text.secondary">
            {totalCount} customers
          </Typography>
          <Pagination
            count={totalPages}
            page={page}
            onChange={(_, value) => setPage(value)}
            color="primary"
            showFirstButton
            showLastButton
            disabled={totalPages === 0}
          />
        </Box>
      </TableContainer>
    </>
  );
}

const StyledTableHeadCell = styled(TableCell)(({ theme }) => ({
  [`&.${tableCellClasses.head}`]: {
    backgroundColor: theme.palette.primary.light,
    color: theme.palette.common.white,
  },
}));
