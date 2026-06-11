export interface User {
  id: string
  username: string
  displayName: string
  role: 'admin' | 'operator' | 'viewer'
}

export interface AuthState {
  user: User | null
  accessToken: string | null
  refreshToken: string | null
}

export interface EnvVar {
  key: string
  val: string
}

export interface Application {
  id: string
  name: string
  type: string
  repoUrl: string
  branch: string
  buildCmd?: string
  outputDir?: string
  trigger: string
  envVars?: string
  deployDir?: string
  serviceName?: string
  port?: number
  scheduleCron?: string
  scheduleEnabled: boolean
  createdAt: string
}

export interface DeployJob {
  id: string
  appId: string
  status: string
  triggeredBy: string
  triggeredByUser?: string
  commitHash?: string
  commitMsg?: string
  createdAt: string
  startedAt?: string
  finishedAt?: string
}

export interface DeployLog {
  timestamp: string
  message: string
  level: string
}

export interface ServerSummary {
  id: string
  name: string
  ip: string
  sshPort: number
  role: string
  hostname?: string
  tags?: string
  nginxInstalled: boolean
  createdAt: string
}
