import { TableCell, styled, tableCellClasses } from "@mui/material";

const StyledTableHeadCell = styled(TableCell)(({ theme }) => ({
  [`&.${tableCellClasses.head}`]: {
    backgroundColor: theme.palette.primary.light,
    color: theme.palette.common.white,
  },
}));

export default StyledTableHeadCell;
