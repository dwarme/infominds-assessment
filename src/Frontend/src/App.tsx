import { Container } from "@mui/material";
import AppRoutes from "./routing/AppRouter";
import ShellHeader from "./routing/ShellHeader";
import ChatWidget from "./components/ChatWidget";
import CssBaseline from "@mui/material/CssBaseline";
import { BrowserRouter as Router } from "react-router-dom";

function App() {
  return (
    <>
      <CssBaseline />
      <Router>
        <ShellHeader />
        <Container sx={{pl:1}}>
        <AppRoutes />
        </Container>
        <ChatWidget />
      </Router>
    </>
  );
}

export default App;
