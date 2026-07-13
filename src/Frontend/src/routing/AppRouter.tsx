import { Routes, Route } from "react-router-dom";
import HomePage from "../pages/HomePage";
import CustomerListPage from "../pages/CustomerListPage";
import CustomerDetailPage from "../pages/CustomerDetailPage";
import EmployeeListPage from "../pages/EmployeeListPage";
import SupplierListPage from "../pages/SupplierListPage";
import SupplierDetailPage from "../pages/SupplierDetailPage";

export default function AppRoutes() {
  return (
    <Routes>
      <Route path="/" element={<HomePage />} />
      <Route path="/SupplierList" element={<SupplierListPage />} />
      <Route path="/suppliers/:id" element={<SupplierDetailPage />} />
      <Route path="/CustomerList" element={<CustomerListPage />} />
      <Route path="/customers/:id" element={<CustomerDetailPage />} />
      <Route path="/EmployeeList" element={<EmployeeListPage />} />
    </Routes>
  );
}
