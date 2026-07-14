import { Box, Pagination, Typography } from "@mui/material";

interface ListPaginationFooterProps {
  totalCount: number;
  totalPages: number;
  page: number;
  onPageChange: (page: number) => void;
  itemLabel: string;
}

export default function ListPaginationFooter({
  totalCount,
  totalPages,
  page,
  onPageChange,
  itemLabel,
}: ListPaginationFooterProps) {
  return (
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
        {totalCount} {itemLabel}
      </Typography>
      <Pagination
        count={totalPages}
        page={page}
        onChange={(_, value) => onPageChange(value)}
        color="primary"
        showFirstButton
        showLastButton
        disabled={totalPages === 0}
      />
    </Box>
  );
}
