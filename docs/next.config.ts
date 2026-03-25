import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  output: "export",

  /**
   * Set base path to match the GitHub repository name.
   * Change this value if your repository has a different name.
   */
  basePath: "/adb-latest-driver-installer",

  env: {
    NEXT_PUBLIC_BASE_PATH: "/adb-latest-driver-installer",
  },

  images: {
    unoptimized: true,
  },

  /** Fix Turbopack font fetching when system has custom TLS config */
  experimental: {
    turbopackUseSystemTlsCerts: true,
  },
};

export default nextConfig;
