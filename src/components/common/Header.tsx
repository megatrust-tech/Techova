"use client";

import Image from "next/image";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { logout } from "@/lib/api/auth";

type Locale = "en" | "ar";

type HeaderProps = {
  locale: Locale;
  onToggleLocale?: (next: Locale) => void;
  labels: {
    home: string;
    why: string;
    pricing: string;
    resources: string;
    language: string;
  };
  showLogout?: boolean;
};

export default function Header({
  locale,
  onToggleLocale,
  labels,
  showLogout = false,
}: HeaderProps) {
  const router = useRouter();
  const altLocale: Locale = locale === "en" ? "ar" : "en";
  const flagSrc = altLocale === "en" ? "/en.svg" : "/ar.svg";
  const altLabel = altLocale === "en" ? "English" : "Arabic";

  const handleLogout = async () => {
    await logout();
    router.push("/login");
  };

  return (
    <header className="site-header">
      <div className="site-header__left">
        <div className="brand">
          <Image
            src="/taskedin-logo.svg"
            alt="TaskedIn"
            width={156}
            height={36}
            priority
          />
        </div>
        <nav className="site-nav">
          <Link href="/">{labels.home}</Link>
          <Link href="/#why">{labels.why}</Link>
          <Link href="/#pricing">{labels.pricing}</Link>
          <Link href="/#resources">{labels.resources}</Link>
        </nav>
      </div>
      <div style={{ display: "flex", alignItems: "center", gap: "1rem" }}>
        {showLogout && (
          <button
            type="button"
            onClick={handleLogout}
            className="lang-toggle"
            style={{ marginRight: 0 }}
          >
            Logout
          </button>
        )}
        <button
          type="button"
          className="lang-toggle header-lang"
          aria-label={`Switch to ${altLocale === "en" ? "English" : "Arabic"}`}
          onClick={() => onToggleLocale?.(altLocale)}
        >
          <span className="lang-text">
            {altLabel === "English" ? "English" : "العربية"}
          </span>
          <span className="lang-flag">
            <Image src={flagSrc} alt={altLabel} width={24} height={16} />
          </span>
        </button>
      </div>
    </header>
  );
}