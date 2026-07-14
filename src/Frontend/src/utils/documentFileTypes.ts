const ALLOWED_EXTENSIONS = [".txt", ".md"];
export const MAX_DOCUMENT_FILE_SIZE_BYTES = 1024 * 1024;
export const MAX_DOCUMENT_FILE_SIZE_LABEL = "1 MB";

export function isAllowedDocumentFile(file: File): boolean {
  const name = file.name.toLowerCase();
  return ALLOWED_EXTENSIONS.some((ext) => name.endsWith(ext));
}

export function isWithinDocumentFileSizeLimit(file: File): boolean {
  return file.size > 0 && file.size <= MAX_DOCUMENT_FILE_SIZE_BYTES;
}

export const DOCUMENT_FILE_ACCEPT = ".txt,.md,text/plain,text/markdown";
