import { useEffect, useState } from "react";
import { PAGE_SIZE, PaginatedResponse } from "../types/pagination";

export function usePaginatedList<T>(endpoint: string, debouncedSearchText: string) {
  const [list, setList] = useState<T[]>([]);
  const [page, setPage] = useState(1);
  const [totalCount, setTotalCount] = useState(0);
  const [totalPages, setTotalPages] = useState(0);

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

    fetch(`${endpoint}?${params.toString()}`)
      .then((response) => response.json())
      .then((data) => {
        const result = data as PaginatedResponse<T>;
        setList(result.items);
        setTotalCount(result.totalCount);
        setTotalPages(result.totalPages);
      });
  }, [endpoint, debouncedSearchText, page]);

  return { list, page, setPage, totalCount, totalPages };
}
