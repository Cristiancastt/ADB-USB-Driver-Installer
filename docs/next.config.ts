import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  output: "export",

  /**
   * Set base path to match the GitHub repository name.
   * Change this value if your repository has a different name.
   */
  basePath: "/ADB-USB-Driver-Installer",

  env: {
    NEXT_PUBLIC_BASE_PATH: "/ADB-USB-Driver-Installer",
  },

  images: {
    unoptimized: true,
  }
};

export default nextConfig;
