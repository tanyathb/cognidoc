import { useState, useRef } from "react";
import { BlockBlobClient } from "@azure/storage-blob";
import { styles } from "./FileUpload.styles";

export const FileUpload: React.FC = () => {
    const [file, setFile] = useState<File | null>(null);
    const [progress, setProgress] = useState<number>(0);
    const [status, setStatus] = useState<string>("Ready to upload");
    const [isDragActive, setIsDragActive] = useState<boolean>(false);
    const fileInputRef = useRef<HTMLInputElement>(null);

    const API_URL = import.meta.env.VITE_API_URL || "https://localhost:7001";

    const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
        if (e.target.files && e.target.files.length > 0) {
            setFile(e.target.files[0]);
            setProgress(0);
            setStatus("File staging complete");
        }
    };

    const handleDrag = (e: React.DragEvent) => {
        e.preventDefault();
        e.stopPropagation();
        if (e.type === "dragenter" || e.type === "dragover") {
            setIsDragActive(true);
        } else if (e.type === "dragleave") {
            setIsDragActive(false);
        }
    };

    const handleDrop = (e: React.DragEvent) => {
        e.preventDefault();
        e.stopPropagation();
        setIsDragActive(false);

        if (e.dataTransfer.files && e.dataTransfer.files.length > 0) {
            setFile(e.dataTransfer.files[0]);
            setProgress(0);
            setStatus("File staging complete");
        }
    };

    const handleUpload = async () => {
        if (!file) {
            setStatus("Validation error: No file selected.");
            return;
        }

        // try {
        //     setStatus("Authenticating payload secure token...");

        //     const response = await fetch(`${API_URL}/api/upload/initiate`, {
        //         method: "POST",
        //         headers: { "Content-Type": "application/json" },
        //         body: JSON.stringify({
        //             filename: file.name,
        //             fileSizeBytes: file.size,
        //             contentType: file.type || "application/octet-stream"
        //         })
        //     });

        //     if (!response.ok) throw new Error("API rejection creating secure cloud SAS ticket.");

        //     const { sasUrl } = await response.json();
        //     setStatus("Streaming telemetry payload blocks...");

        //     const blobClient = new BlockBlobClient(sasUrl);
        //     await blobClient.uploadData(file, {
        //         blockSize: 4 * 1024 * 1024,
        //         concurrency: 4,
        //         onProgress: (ev) => {
        //             setProgress((ev.loadedBytes / file.size) * 100);
        //         }
        //     });

        //     setStatus("Payload synced. Processing background AI worker orchestration pipeline...");
        //     setFile(null);
        //     if (fileInputRef.current) fileInputRef.current.value = "";

        // } catch (error: any) {
        //     console.error(error);
        //     setStatus(`Execution halted: ${error.message}`);
        // }

        try {
            setStatus("Streaming telemetry payload directly through Web API gateway...");
            setProgress(20); // Simulate network startup block tracking

            // Package the file block inside standard browser multipart form metrics
            const formData = new FormData();
            formData.append("file", file);

            // Call our server-side local proxy route instead of hitting the raw storage port
            const response = await fetch(`${API_URL}/api/Upload/local-direct-upload`, {
                method: "POST",
                body: formData
                // Note: Do NOT set Content-Type header manually here; the browser automatically sets multipart/form-data
            });

            if (!response.ok) {
                const errorText = await response.text();
                throw new Error(`API Gateway rejection: ${errorText}`);
            }

            setProgress(100);
            setStatus("Payload synced. Server side storage write verified!");

            setFile(null);
            if (fileInputRef.current) fileInputRef.current.value = "";

        } catch (error: any) {
            console.error(error);
            setStatus(`Execution halted: ${error.message}`);
        }
    };

    return (
        <div style={styles.container}>
            <h2 style={styles.title}>Document Ingestion Node</h2>
            <p style={styles.subtitle}>Upload enterprise unstructured datasets to the vector grid pipeline.</p>

            <div
                style={{
                    ...styles.dropzone,
                    ...(isDragActive ? styles.dropzoneActive : {})
                }}
                onDragEnter={handleDrag}
                onDragOver={handleDrag}
                onDragLeave={handleDrag}
                onDrop={handleDrop}
            >
                <div style={styles.iconContainer}>📁</div>
                <p style={styles.browseText}>
                    Drag & drop your file here, or <span style={styles.linkText}>browse</span>
                </p>
                <p style={styles.hint}>Supported extensions: PDF, TXT, DOCX, CSV up to 500MB</p>

                <input
                    type="file"
                    onChange={handleFileChange}
                    ref={fileInputRef}
                    accept=".pdf,.txt,.docx,.csv"
                    style={styles.hiddenInput}
                />
            </div>

            {file && (
                <div style={styles.fileCard}>
                    <div style={styles.fileInfo}>
                        📄 <strong>{file.name}</strong> ({(file.size / 1024 / 1024).toFixed(2)} MB)
                    </div>
                    <button onClick={handleUpload} style={styles.uploadButton}>
                        Push Payload
                    </button>
                </div>
            )}

            {progress > 0 && (
                <div style={styles.progressWrapper}>
                    <div style={styles.progressLabels}>
                        <span>Uploading...</span>
                        <span>{progress.toFixed(0)}%</span>
                    </div>
                    <div style={styles.progressContainer}>
                        <div style={{ ...styles.progressBar, width: `${progress}%` }} />
                    </div>
                </div>
            )}

            <div style={styles.statusBadge}>
                🌐 <strong>System Node Status:</strong> {status}
            </div>
        </div>
    );
};
