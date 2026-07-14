import { useEffect, useMemo, useState } from "react";
import { debounce } from "../utils/debounce";

export function useDebouncedSearch(delayMs = 300) {
  const [searchText, setSearchText] = useState("");
  const [debouncedSearchText, setDebouncedSearchText] = useState("");

  const debouncedSetSearch = useMemo(
    () =>
      debounce((search: string) => {
        setDebouncedSearchText(search);
      }, delayMs),
    [delayMs]
  );

  useEffect(() => {
    debouncedSetSearch(searchText);

    return () => {
      debouncedSetSearch.cancel();
    };
  }, [searchText, debouncedSetSearch]);

  return { searchText, setSearchText, debouncedSearchText };
}
