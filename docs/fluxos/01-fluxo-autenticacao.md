# Fluxo de Autenticacao

## Cadastro

```text
Nome + email + senha
  -> validar email unico
  -> salvar senha com hash
  -> criar usuario
```

## Login

```text
Email + senha
  -> validar credenciais
  -> gerar JWT
  -> frontend salva token
  -> chamadas privadas enviam token
```

