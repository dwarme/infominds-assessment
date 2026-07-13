import AppBar from "@mui/material/AppBar";
import Box from "@mui/material/Box";
import Button, { ButtonProps } from "@mui/material/Button";
import Toolbar from "@mui/material/Toolbar";
import Typography from "@mui/material/Typography";
import { Link as RouterLink } from "react-router-dom";

export default function ShellHeader() {
  return (
    <Box sx={{ flexGrow: 1 }}>
      <AppBar position="static">
        <Toolbar>
          <HomeButton />
          <Box sx={{ flexGrow: 1, display: "flex" }}>
            <HeaderButton to="/SupplierList">Suppliers</HeaderButton>
            <HeaderButton to="/CustomerList">Customers</HeaderButton>
            <HeaderButton to="/EmployeeList">Employees</HeaderButton>
            <HeaderLinkButton href="/swagger" variant="outlined">Swagger UI</HeaderLinkButton>
          </Box>
        </Toolbar>
      </AppBar>
    </Box>
  );
}

function HomeButton() {
  return (
    <Typography
      component={RouterLink}
      to="/"
      variant="h6"
      sx={{ textDecoration: "none", color: "white" }}
    >
      Test Application
    </Typography>
  );
}

interface HeaderButtonProps extends ButtonProps {
  to: string;
}

function HeaderButton(props: HeaderButtonProps) {
  const { to, ...other } = props;

  return (
    <Button
      component={RouterLink}
      to={to}
      {...other}
      sx={{ my: 2, color: "white", display: "block" }}
    ></Button>
  );
}

function HeaderLinkButton(props: ButtonProps) {
  return (
    <Button
      {...props}
      sx={{ my: 2, color: "white", display: "block" }}
    ></Button>
  );
}
