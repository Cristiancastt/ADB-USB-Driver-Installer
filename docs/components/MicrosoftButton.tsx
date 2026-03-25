export default function MicrosoftButton({ label }: { label: string }) {
  return (
    <a
      href="https://dotnet.microsoft.com/download/dotnet/10.0"
      target="_blank"
      rel="noopener noreferrer"
      className="brutalist-button"
    >
      <div className="ms-logo">
        <div className="ms-logo-square" />
        <div className="ms-logo-square" />
        <div className="ms-logo-square" />
        <div className="ms-logo-square" />
      </div>
      <div className="button-text">
        <span>{label}</span>
        <span>Microsoft</span>
      </div>
    </a>
  );
}
