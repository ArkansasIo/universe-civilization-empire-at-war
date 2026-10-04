import "dotenv/config";
import express, { type NextFunction, type Request, type Response } from "express";
import session from "express-session";
import MemoryStoreFactory from "memorystore";
import crypto from "node:crypto";
import fs from "node:fs";
import path from "node:path";
import { checkDatabase, db, shutdownDb } from "./db";
import { users } from "../shared/schema";
import { eq, ilike, or, sql } from "drizzle-orm";
import { registerAdminTerminalRoutes } from "./routes-admin-terminal";

const app = express();
const port = Number.parseInt(process.env.PORT || "5001", 10);
const SessionStore = MemoryStoreFactory(session);
const distDir = path.resolve(process.cwd(), "dist");
const indexFile = path.join(distDir, "index.html");

app.set("trust proxy", 1);
app.use(express.json({ limit: "1mb" }));
app.use(express.urlencoded({ extended: false }));
app.use(session({
  name: "connect.sid",
  secret: process.env.SESSION_SECRET || crypto.randomBytes(32).toString("hex"),
  store: new SessionStore({ checkPeriod: 86_400_000 }),
  resave: false,
  saveUninitialized: false,
  cookie: {
    httpOnly: true,
    sameSite: "lax",
    secure: process.env.NODE_ENV === "production",
    maxAge: 7 * 24 * 60 * 60 * 1000,
  },
}));

function hashPassword(password: string): string {
  return crypto.createHash("sha256").update(password).digest("hex");
}

app.get("/api/status/health", async (_req, res) => {
  try {
    await db.execute(sql`SELECT 1`);
    res.json({ ok: true, status: "healthy", timestamp: new Date().toISOString() });
  } catch {
    res.status(503).json({ ok: false, status: "degraded", timestamp: new Date().toISOString() });
  }
});

app.post("/api/auth/login", async (req, res) => {
  try {
    const identifier = String(req.body?.username || req.body?.email || "").trim();
    const password = String(req.body?.password || "");
    if (!identifier || !password) {
      return res.status(400).json({ message: "Username/email and password are required" });
    }

    const [user] = await db.select().from(users).where(
      identifier.includes("@")
        ? ilike(users.email, identifier)
        : or(ilike(users.username, identifier), ilike(users.email, identifier)),
    ).limit(1);

    if (!user || user.passwordHash !== hashPassword(password)) {
      return res.status(401).json({ message: "Invalid credentials" });
    }
    if (user.isBanned) {
      return res.status(403).json({ message: user.banReason || "Account is banned" });
    }

    (req.session as any).userId = user.id;
    await new Promise<void>((resolve, reject) => req.session.save(err => err ? reject(err) : resolve()));
    res.json({ message: "Login successful", user: { id: user.id, username: user.username, email: user.email } });
  } catch (error) {
    res.status(500).json({ message: error instanceof Error ? error.message : "Login failed" });
  }
});

app.post("/api/auth/logout", (req, res) => {
  req.session.destroy(() => res.json({ ok: true }));
});

app.get("/api/auth/me", async (req, res) => {
  if (!(req.session as any).userId) return res.status(401).json({ authenticated: false });
  const [user] = await db.select({ id: users.id, username: users.username, email: users.email, isBanned: users.isBanned })
    .from(users).where(eq(users.id, (req.session as any).userId)).limit(1);
  if (!user || user.isBanned) return res.status(401).json({ authenticated: false });
  res.json({ authenticated: true, user });
});

registerAdminTerminalRoutes(app);

app.use(express.static(distDir));

app.get("*", (req, res, next) => {
  if (req.path.startsWith("/api/")) return next();
  if (fs.existsSync(indexFile)) return res.sendFile(indexFile);
  res.status(503).json({
    ok: false,
    message: "Frontend build not found.",
    hint: "Run npm run build before starting the production server.",
    expected: indexFile,
  });
});

app.use((err: unknown, _req: Request, res: Response, _next: NextFunction) => {
  if (res.headersSent) return;
  res.status(500).json({ ok: false, message: err instanceof Error ? err.message : "Internal Server Error" });
});

const server = app.listen(port, "0.0.0.0", () => {
  console.log("================================================");
  console.log(" UNIVERSE CIVILIZATION SERVER");
  console.log("================================================");
  console.log("API:    http://localhost:" + port + "/api");
  console.log("Health: http://localhost:" + port + "/api/status/health");
  console.log("Admin:  http://localhost:" + port + "/api/admin/terminal/menu");
  console.log("Web:    http://localhost:" + port + "/");
});

server.on("error", (error: NodeJS.ErrnoException) => {
  if (error.code === "EADDRINUSE") {
    console.error(`Port ${port} is already in use.`);
    console.error(`Stop the existing server or start this server on another port, for example: PORT=${port + 1}`);
    process.exitCode = 1;
    return;
  }
  console.error("Server failed to start:", error);
  process.exitCode = 1;
});

const shutdown = async (signal: string) => {
  console.log("Received " + signal + "; shutting down...");
  server.close(async () => {
    await shutdownDb();
    process.exit(0);
  });
};
process.on("SIGINT", () => void shutdown("SIGINT"));
process.on("SIGTERM", () => void shutdown("SIGTERM"));
