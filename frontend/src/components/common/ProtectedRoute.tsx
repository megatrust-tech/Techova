"use client";

import { useEffect, useState } from "react";
import { useRouter, usePathname } from "next/navigation";
import { isAuthenticated } from "@/lib/api/auth";

interface ProtectedRouteProps {
  children: React.ReactNode;
}

const publicRoutes = ["/login", "/register"];

export default function ProtectedRoute({ children }: ProtectedRouteProps) {
  const router = useRouter();
  const pathname = usePathname();
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    // Allow access to public routes
    if (publicRoutes.includes(pathname)) {
      setIsLoading(false);
      return;
    }

    // Check authentication for protected routes
    if (!isAuthenticated()) {
      router.push("/login");
    } else {
      setIsLoading(false);
    }
  }, [pathname, router]);

  // Show loading state while checking auth
  if (isLoading) {
    return (
      <main className="flex items-center justify-center min-h-screen">
        <div className="text-center">
          <div className="text-4xl mb-4">Loading...</div>
        </div>
      </main>
    );
  }

  // Allow public routes to render without protection
  if (publicRoutes.includes(pathname)) {
    return <>{children}</>;
  }

  // Only render children if authenticated
  if (isAuthenticated()) {
    return <>{children}</>;
  }

  // This shouldn't render, but just in case
  return null;
}
