import { AppError, type TipoErro } from '@/application/erros'

/** Formato RFC 7807 que o ASP.NET devolve com Results.Problem / ValidationProblem. */
interface ProblemDetails {
  title?: string
  detail?: string
  status?: number
  errors?: Record<string, string[]>
}

const TIPO_POR_STATUS: Record<number, TipoErro> = {
  400: 'validacao',
  401: 'nao_autorizado',
  404: 'nao_encontrado',
  409: 'conflito',
  422: 'regra_negocio',
}

export type HttpClient = ReturnType<typeof criarHttpClient>

export function criarHttpClient(baseUrl: string) {
  async function request<T>(method: string, caminho: string, corpo?: unknown): Promise<T> {
    let resposta: Response
    try {
      resposta = await fetch(baseUrl + caminho, {
        method,
        headers: corpo === undefined ? undefined : { 'Content-Type': 'application/json' },
        body: corpo === undefined ? undefined : JSON.stringify(corpo),
      })
    } catch {
      throw new AppError('rede', 'Não foi possível conectar à API. Ela está rodando?')
    }

    const texto = await resposta.text()
    const json: unknown = texto ? tentarJson(texto) : undefined

    if (!resposta.ok) throw paraAppError(resposta.status, json)
    return json as T
  }

  return {
    get: <T>(caminho: string) => request<T>('GET', caminho),
    post: <T>(caminho: string, corpo?: unknown) => request<T>('POST', caminho, corpo ?? {}),
    put: <T>(caminho: string, corpo: unknown) => request<T>('PUT', caminho, corpo),
  }
}

function tentarJson(texto: string): unknown {
  try {
    return JSON.parse(texto)
  } catch {
    return texto
  }
}

function paraAppError(status: number, corpo: unknown): AppError {
  const tipo = TIPO_POR_STATUS[status] ?? 'inesperado'

  // Results.BadRequest("Horário Inválido") devolve só uma string.
  if (typeof corpo === 'string' && corpo) return new AppError(tipo, corpo)

  const problema = (corpo ?? {}) as ProblemDetails
  const detalhes = Object.values(problema.errors ?? {}).flat()
  const mensagem =
    problema.detail ?? detalhes[0] ?? problema.title ?? `Erro ${status} ao chamar a API.`
  return new AppError(tipo, mensagem, detalhes)
}
