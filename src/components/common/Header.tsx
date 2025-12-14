"use client";

import Image from "next/image";
import Link from "next/link";

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
};

export default function Header({
  locale,
  onToggleLocale,
  labels,
}: HeaderProps) {
  const altLocale: Locale = locale === "en" ? "ar" : "en";
  const flagSrc = altLocale === "en" ? "/en.svg" : "/ar.svg";
  const altLabel = altLocale === "en" ? "English" : "Arabic";

  return (
    <header className="site-header">
      <div className="site-header__left">
        <Link href="/" className="brand">
          <Image
            src="/taskedin-logo.svg"
            alt="TaskedIn"
            width={156}
            height={36}
            priority
          />
        </Link>
        <nav className="site-nav">
          <Link href="/">{labels.home}</Link>
          <Link href="/#why">{labels.why}</Link>
          <Link href="/#pricing">{labels.pricing}</Link>
          <Link href="/#resources">{labels.resources}</Link>
        </nav>
      </div>
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
    </header>
  );
}
