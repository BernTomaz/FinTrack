# Fluxo de autenticação

## Cadastro

```text
Nome + e-mail + senha
  -> validar e-mail único
  -> validar nome único
  -> validar senha forte
  -> salvar senha com hash
  -> criar usuário
  -> primeiro usuário vira administrador
```

## Login

```text
E-mail + senha
  -> validar credenciais
  -> registrar tentativa inválida quando falhar
  -> bloquear conta após excesso de tentativas
  -> gerar JWT
  -> frontend salva token
  -> chamadas privadas enviam token
```

## Perfil e senha

```text
Usuário autenticado
  -> consulta perfil
  -> atualiza nome
  -> recebe novo JWT
  -> altera senha informando a senha atual
```

## Reset admin

```text
Administrador
  -> lista usuários bloqueados
  -> define senha temporária forte
  -> usuário bloqueado volta a conseguir login
```

