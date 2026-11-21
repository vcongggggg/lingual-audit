import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  /* Keep route generation within the memory budget of local and CI builds. */
  experimental: { cpus: 2 },
};

export default nextConfig;
