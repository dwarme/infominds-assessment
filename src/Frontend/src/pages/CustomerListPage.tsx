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
    styled,
    tableCellClasses,
  } from "@mui/material";
  import { useEffect, useMemo, useState } from "react";
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
  
  export default function CustomerListPage() {
    const [list, setList] = useState<CustomerListQuery[]>([]);
    const [searchText, setSearchText] = useState("");

    /*
     * Debounce the fetch customers function to prevent multiple requests
     * when the user is typing.
     * This is useful to prevent the server from being overwhelmed by multiple requests.
     */
    const debouncedFetchCustomers = useMemo(
      () =>
        debounce((search: string) => {
          const params = new URLSearchParams();
          if (search) {
            params.set("SearchText", search);
          }

          const query = params.toString();
          const url = query
            ? `/api/customers/list?${query}`
            : "/api/customers/list";

          fetch(url)
            .then((response) => response.json())
            .then((data) => {
              setList(data as CustomerListQuery[]);
            });
        }, 300),
      []
    );

    useEffect(() => {
      debouncedFetchCustomers(searchText);

      // Cancel the debounced function when the component unmounts.
      return () => {
        debouncedFetchCustomers.cancel();
      };
    }, [searchText, debouncedFetchCustomers]);
  
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
                  <TableCell>{row.name}</TableCell>
                  <TableCell>{row.address}</TableCell>
                  <TableCell>{row.email}</TableCell>
                  <TableCell>{row.phone}</TableCell>
                  <TableCell>{row.iban}</TableCell>
                  <TableCell>{row.customerCategory?.description ?? "-"}</TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
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
  