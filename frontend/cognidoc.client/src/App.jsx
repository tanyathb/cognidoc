
import { FileUpload } from "./components/FileUpload";

function App() {
    return (
        <div style={styles.appContainer}>
            <header style={styles.header}>
                <h2>CogniDoc Document Processing Application</h2>
            </header>
            <main style={styles.mainContent}>
                {/* 🚀 Rendering your upload component here */}
                <FileUpload />
            </main>
        </div>
    );
}

const styles = {
    appContainer: {
        backgroundColor: "#faf9f8",
        minHeight: "100vh",
        fontFamily: "Segoe UI, sans-serif"
    },
    header: {
        backgroundColor: "#0078d4",
        color: "white",
        padding: "15px 20px",
        boxShadow: "0 2px 4px rgba(0,0,0,0.1)"
    },
    mainContent: {
        padding: "4px"
    }
};

export default App;
