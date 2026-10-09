/** Date -> "AAAA-MM-DD" no fuso local (toISOString usaria UTC e poderia virar o dia). */
export function paraDataIso(data: Date): string {
  const p = (n: number) => String(n).padStart(2, '0')
  return `${data.getFullYear()}-${p(data.getMonth() + 1)}-${p(data.getDate())}`
}

/** "AAAA-MM-DD" + hora -> Date no fuso local. */
export function combinarDataHora(dataIso: string, horas: number, minutos = 0): Date {
  const [ano, mes, dia] = dataIso.split('-').map(Number)
  return new Date(ano, mes - 1, dia, horas, minutos)
}

export function somarMinutos(data: Date, minutos: number): Date {
  return new Date(data.getTime() + minutos * 60_000)
}
