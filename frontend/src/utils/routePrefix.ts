let _routeSalt: string | null = null

export function setRouteSalt(routeSalt: string) {
  _routeSalt = routeSalt
}

export function getRouteSalt(): string {
  return _routeSalt || ''
}