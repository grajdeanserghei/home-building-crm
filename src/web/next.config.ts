import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  // Required by Aspire's AddNextJsApp for the published standalone container image.
  output: "standalone",
};

export default nextConfig;
