function getFilenameFromDisposition(disposition: string | null, fallback: string): string {
  if (!disposition) return fallback;

  const utf8Match = disposition.match(/filename\*=UTF-8''([^;]+)/i);
  if (utf8Match?.[1]) return decodeURIComponent(utf8Match[1]);

  const filenameMatch = disposition.match(/filename="([^"]+)"/i);
  if (filenameMatch?.[1]) return filenameMatch[1];

  return fallback;
}

export async function downloadDocument(url: string, fallbackFilename: string): Promise<void> {
  const response = await fetch(url);
  if (!response.ok) return;

  const blob = await response.blob();
  const filename = getFilenameFromDisposition(
    response.headers.get("Content-Disposition"),
    fallbackFilename,
  );

  const blobUrl = URL.createObjectURL(blob);
  const link = document.createElement("a");
  link.href = blobUrl;
  link.download = filename;
  link.click();
  URL.revokeObjectURL(blobUrl);
}
