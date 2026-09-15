// src/components/FileUpload.styles.ts

export const styles: { [key: string]: React.CSSProperties } = {
    container: {
        backgroundColor: "#ffffff",
        padding: "32px",
        borderRadius: "16px",
        boxShadow: "0 4px 20px rgba(0, 0, 0, 0.05), 0 1px 3px rgba(0, 0, 0, 0.02)",
        maxWidth: "560px",
        margin: "40px auto",
        fontFamily: "system-ui, -apple-system, sans-serif",
        border: "1px solid #f0f0f0"
    },
    title: {
        fontSize: "20px",
        fontWeight: "600",
        color: "#111827",
        margin: "0 0 8px 0",
        textAlign: "center"
    },
    subtitle: {
        fontSize: "14px",
        color: "#6b7280",
        margin: "0 0 24px 0",
        textAlign: "center"
    },
    dropzone: {
        border: "2px dashed #cbd5e1",
        padding: "40px 20px",
        textAlign: "center",
        borderRadius: "12px",
        backgroundColor: "#f8fafc",
        cursor: "pointer",
        transition: "all 0.2s ease-in-out",
        position: "relative"
    },
    dropzoneActive: {
        border: "2px dashed #3b82f6",
        backgroundColor: "#eff6ff"
    },
    iconContainer: {
        fontSize: "36px",
        marginBottom: "12px",
        color: "#94a3b8"
    },
    hiddenInput: {
        position: "absolute",
        top: 0,
        left: 0,
        width: "100%",
        height: "100%",
        opacity: 0,
        cursor: "pointer"
    },
    browseText: {
        fontSize: "15px",
        fontWeight: "500",
        color: "#374151",
        margin: "0 0 6px 0"
    },
    linkText: {
        color: "#3b82f6",
        textDecoration: "underline"
    },
    hint: {
        fontSize: "12px",
        color: "#94a3b8",
        margin: 0
    },
    fileCard: {
        marginTop: "20px",
        padding: "14px 16px",
        backgroundColor: "#f1f5f9",
        borderRadius: "8px",
        display: "flex",
        justifyContent: "space-between",
        alignItems: "center",
        border: "1px solid #e2e8f0"
    },
    fileInfo: {
        fontSize: "14px",
        color: "#334155",
        whiteSpace: "nowrap",
        overflow: "hidden",
        textOverflow: "ellipsis",
        maxWidth: "70%"
    },
    uploadButton: {
        backgroundColor: "#2563eb",
        color: "white",
        border: "none",
        padding: "10px 18px",
        borderRadius: "8px",
        cursor: "pointer",
        fontWeight: "600",
        fontSize: "14px",
        boxShadow: "0 2px 4px rgba(37, 99, 235, 0.2)",
        transition: "background-color 0.2s"
    },
    progressWrapper: {
        marginTop: "24px"
    },
    progressLabels: {
        display: "flex",
        justifyContent: "space-between",
        fontSize: "13px",
        fontWeight: "500",
        color: "#475569",
        marginBottom: "6px"
    },
    progressContainer: {
        backgroundColor: "#e2e8f0",
        borderRadius: "9999px",
        height: "8px",
        overflow: "hidden"
    },
    progressBar: {
        backgroundColor: "#2563eb",
        height: "100%",
        borderRadius: "9999px",
        transition: "width 0.1s ease-out"
    },
    statusBadge: {
        marginTop: "20px",
        padding: "10px 14px",
        borderRadius: "8px",
        fontSize: "13px",
        fontWeight: "500",
        backgroundColor: "#f4f4f5",
        color: "#52525b",
        border: "1px solid #e4e4e7"
    }
};
